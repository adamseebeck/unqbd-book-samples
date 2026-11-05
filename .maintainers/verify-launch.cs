// Launch recheck for the companion code. Run from the repository root with the released .NET 11 SDK:
//
//   dotnet run .maintainers/verify-launch.cs
//
// It builds every project and solution, runs every test project, checks that the 11.0-resolute
// container image tags used by the samples resolve on mcr.microsoft.com and carry a released
// (not preview or rc) .NET version, then prints a pass/fail table per book. Exit code 0 means
// everything passed. Options (after --):
//   --root <dir>       verify this folder instead of the checkout that holds this script
//   --books <a,b>      only these book folders (prefix match, for example --books blazor,efcore)
//   --parallel <n>     concurrent build/test groups (default: half the logical processors)
//   --skip-images      skip the container image check
//   --images-only      only run the container image check
//   --logs <dir>       where per-project logs go (default: a new folder under the temp directory)
//
// Projects are grouped by chapter folder. Projects inside a group build one after another so
// shared project references are never built by two processes at once; groups run in parallel.
// Anything that fails is retried once on its own before it is reported as a failure.
// MAUI platform heads build only when a maui workload is installed (Windows target on Windows,
// Android elsewhere); otherwise they are reported as skipped. A project whose nearest global.json pins SDK 11.0.999 is a deliberate
// broken starting point for a reader activity, so it is expected to fail SDK resolution.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var argv = args.ToList();
string? Opt(string name) { var i = argv.IndexOf(name); return i >= 0 && i + 1 < argv.Count ? argv[i + 1] : null; }
bool Flag(string name) => argv.Contains(name);

var scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Directory.GetCurrentDirectory();
var root = Path.GetFullPath(Opt("--root") ?? (Path.GetFileName(scriptDir) == ".maintainers" ? Path.GetDirectoryName(scriptDir)! : Directory.GetCurrentDirectory()));
var bookFilter = (Opt("--books") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var parallel = int.TryParse(Opt("--parallel"), out var p) && p > 0 ? p : Math.Max(1, Environment.ProcessorCount / 2);
var logRoot = Path.GetFullPath(Opt("--logs") ?? Path.Combine(Path.GetTempPath(), "unqbd-verify-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")));
var imagesOnly = Flag("--images-only");
Directory.CreateDirectory(logRoot);

Console.WriteLine($"Repository: {root}");
Console.WriteLine($"Logs:       {logRoot}");

var sdk = (await Run("dotnet", ["--version"], root, TimeSpan.FromMinutes(2))).Output.Trim();
Console.WriteLine($"SDK:        {sdk}");
if (sdk.Contains('-'))
    Console.WriteLine("WARNING: this is a prerelease SDK. The global.json files request the released 11.0.100 SDK.");
var workloads = (await Run("dotnet", ["workload", "list"], root, TimeSpan.FromMinutes(2))).Output;
var hasMaui = Regex.IsMatch(workloads, @"^\s*maui", RegexOptions.Multiline | RegexOptions.IgnoreCase);
Console.WriteLine($"MAUI:       {(hasMaui ? "workload installed, platform heads build for " + (OperatingSystem.IsWindows() ? "Windows" : "Android") : "workload not installed, platform heads skipped")}");
Console.WriteLine($"Parallel:   {parallel}");
Console.WriteLine();

var books = Directory.GetDirectories(root)
    .Select(Path.GetFileName)
    .Where(n => n is not null && !n.StartsWith('.') && File.Exists(Path.Combine(root, n, "README.md")))
    .Where(n => bookFilter.Length == 0 || bookFilter.Any(f => n!.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
    .Select(n => n!)
    .OrderBy(n => n)
    .ToList();

var results = new ConcurrentBag<Result>();

if (!imagesOnly)
{
    var projects = books.SelectMany(b => Find(Path.Combine(root, b), "*.csproj")).ToList();
    var solutions = books.SelectMany(b => Find(Path.Combine(root, b), "*.sln").Concat(Find(Path.Combine(root, b), "*.slnx"))).ToList();
    var mauiHeads = projects.Where(IsMauiHead).ToHashSet();
    Console.WriteLine($"Found {projects.Count} projects ({mauiHeads.Count} MAUI platform heads) and {solutions.Count} solutions in {books.Count} books.");

    // 1. Projects
    await RunGroups("build", projects, async proj =>
    {
        if (IsFragment(proj)) return Skip("build", proj, "overlay fragment: its ProjectReference only exists after create_copy.py copies it into a checkpoint");
        if (mauiHeads.Contains(proj) && !hasMaui) return Skip("build", proj, "MAUI platform head, maui workload not installed");
        // The MAUI README's own Windows command, or Android elsewhere.
        var mauiTfm = OperatingSystem.IsWindows() ? "net11.0-windows10.0.19041.0" : "net11.0-android";
        string[] extra = mauiHeads.Contains(proj) ? ["-p:BookTargetFrameworks=" + mauiTfm, "-f", mauiTfm] : [];
        return await Step("build", proj, ["build", proj, "-nologo", "-v:q", "-clp:ErrorsOnly", .. extra]);
    });

    // 2. Solutions
    await RunGroups("solution", solutions, async sln =>
    {
        if (!hasMaui && SolutionProjects(sln).Any(mauiHeads.Contains)) return Skip("solution", sln, "contains a MAUI platform head, maui workload not installed");
        return await Step("solution", sln, ["build", sln, "-nologo", "-v:q", "-clp:ErrorsOnly"]);
    });

    // 3. Tests
    var tests = projects.Where(t => IsTest(t) && !mauiHeads.Contains(t) && !IsFragment(t)).ToList();
    await RunGroups("test", tests, async t =>
    {
        // Microsoft.Testing.Platform projects run as executables, which works whatever the
        // nearest global.json says; VSTest projects use dotnet test.
        string[] cmd = IsMtp(t) ? ["run", "--project", t] : ["test", t, "-nologo"];
        return await Step("test", t, cmd, TimeSpan.FromMinutes(20));
    });
}

// 4. Container images
var images = new List<ImageResult>();
if (!Flag("--skip-images"))
{
    var refs = new SortedSet<string> { "mcr.microsoft.com/dotnet/sdk:11.0-resolute", "mcr.microsoft.com/dotnet/aspnet:11.0-resolute", "mcr.microsoft.com/dotnet/runtime:11.0-resolute", "mcr.microsoft.com/dotnet/runtime-deps:11.0-resolute" };
    foreach (var b in books)
        foreach (var f in Directory.EnumerateFiles(Path.Combine(root, b), "*", SearchOption.AllDirectories).Where(f => !IsBuildOutput(f) && (Path.GetFileName(f).StartsWith("Dockerfile", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".csproj") || f.EndsWith(".yml") || f.EndsWith(".yaml"))))
            foreach (Match m in Regex.Matches(File.ReadAllText(f), @"mcr\.microsoft\.com/dotnet/[a-z0-9/-]+:11\.0-resolute[a-z0-9-]*"))
                refs.Add(m.Value);
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    foreach (var r in refs) images.Add(await CheckImage(http, r));
}

// Report
Console.WriteLine();
var all = results.ToList();
var table = new List<string[]> { new[] { "Book", "Projects", "Built", "Build failed", "Skipped", "Solutions ok", "Test projects ok", "Tests passed", "Tests failed", "Result" } };
var overall = true;
foreach (var b in books)
{
    var r = all.Where(x => x.Book == b).ToList();
    var builds = r.Where(x => x.Kind == "build").ToList();
    var slns = r.Where(x => x.Kind == "solution").ToList();
    var tst = r.Where(x => x.Kind == "test").ToList();
    var ok = r.All(x => x.Status is Status.Pass or Status.Skip or Status.ExpectedFail);
    overall &= ok;
    table.Add([
        b,
        builds.Count.ToString(),
        builds.Count(x => x.Status is Status.Pass).ToString(),
        builds.Count(x => x.Status is Status.Fail).ToString(),
        builds.Count(x => x.Status is Status.Skip or Status.ExpectedFail).ToString(),
        $"{slns.Count(x => x.Status is Status.Pass)}/{slns.Count(x => x.Status is not Status.Skip)}",
        $"{tst.Count(x => x.Status is Status.Pass)}/{tst.Count}",
        tst.Sum(x => x.Passed).ToString(),
        tst.Sum(x => x.Failed).ToString(),
        imagesOnly ? "-" : ok ? "PASS" : "FAIL"]);
}
if (!imagesOnly) PrintTable(table);

if (images.Count > 0)
{
    Console.WriteLine();
    var it = new List<string[]> { new[] { "Image", "Resolves", ".NET version", "Result" } };
    foreach (var i in images)
    {
        overall &= i.Pass;
        it.Add([i.Reference, i.Resolves ? "yes" : "no", i.Version, i.Pass ? "PASS" : "FAIL"]);
    }
    PrintTable(it);
}

var failures = all.Where(x => x.Status is Status.Fail).OrderBy(x => x.Path).ToList();
if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Failures:");
    foreach (var f in failures) Console.WriteLine($"  [{f.Kind}] {f.Path}{Environment.NewLine}      {f.Detail}{Environment.NewLine}      log: {f.Log}");
}
var retried = all.Where(x => x.Retried && x.Status is Status.Pass).ToList();
if (retried.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Passed only on the serial retry (likely a parallel build collision, worth a look):");
    foreach (var f in retried) Console.WriteLine($"  [{f.Kind}] {f.Path}");
}
var skipped = all.Where(x => x.Status is Status.Skip or Status.ExpectedFail).GroupBy(x => x.Detail).ToList();
if (skipped.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Skipped or expected to fail:");
    foreach (var g in skipped) Console.WriteLine($"  {g.Count(),3} x {g.Key}");
}
Console.WriteLine();
Console.WriteLine(overall ? "OVERALL: PASS" : "OVERALL: FAIL");
return overall ? 0 : 1;

// ---------- helpers ----------

async Task RunGroups(string kind, List<string> items, Func<string, Task<Result>> work)
{
    if (items.Count == 0) return;
    var groups = items.GroupBy(GroupKey).Select(g => g.OrderBy(x => x).ToList()).ToList();
    var done = 0;
    var first = new ConcurrentBag<Result>();
    await Parallel.ForEachAsync(groups, new ParallelOptions { MaxDegreeOfParallelism = parallel }, async (g, _) =>
    {
        foreach (var item in g)
        {
            var r = await work(item);
            first.Add(r);
            var n = Interlocked.Increment(ref done);
            Console.WriteLine($"[{kind} {n}/{items.Count}] {Label(r.Status)} {r.Path}");
        }
    });
    foreach (var r in first.Where(r => r.Status is not Status.Fail)) results.Add(r);
    var failed = first.Where(r => r.Status is Status.Fail).Select(r => r.FullPath).OrderBy(x => x).ToList();
    foreach (var item in failed)
    {
        var r = await work(item);
        Console.WriteLine($"[{kind} retry] {Label(r.Status)} {r.Path}");
        results.Add(r with { Retried = true });
    }
}

async Task<Result> Step(string kind, string path, string[] dotnetArgs, TimeSpan? timeout = null)
{
    var dir = Path.GetDirectoryName(path)!;
    var log = Path.Combine(logRoot, kind, Rel(path).Replace('/', '_').Replace('\\', '_') + ".log");
    Directory.CreateDirectory(Path.GetDirectoryName(log)!);
    var run = await Run("dotnet", dotnetArgs, dir, timeout ?? TimeSpan.FromMinutes(15));
    await File.WriteAllTextAsync(log, run.Output);
    var expectedFail = NearestGlobalJsonVersion(dir) == "11.0.999";
    var (passed, failedTests) = kind == "test" ? ParseTests(run.Output) : (0, 0);
    Status status;
    string detail;
    if (expectedFail)
    {
        status = run.ExitCode != 0 ? Status.ExpectedFail : Status.Fail;
        detail = run.ExitCode != 0 ? "deliberate 11.0.999 SDK pin (reader activity start)" : "expected the deliberate 11.0.999 pin to fail, but it built";
    }
    else
    {
        status = run.ExitCode == 0 ? Status.Pass : Status.Fail;
        detail = run.TimedOut ? "timed out" : FirstErrors(run.Output);
    }
    return new Result(kind, BookOf(path), Rel(path), path, status, detail, log, passed, failedTests, false);
}

Result Skip(string kind, string path, string why) => new(kind, BookOf(path), Rel(path), path, Status.Skip, why, "", 0, 0, false);

static async Task<RunResult> Run(string file, string[] arguments, string cwd, TimeSpan timeout)
{
    var psi = new ProcessStartInfo(file) { WorkingDirectory = cwd, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    foreach (var a in arguments) psi.ArgumentList.Add(a);
    psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
    psi.Environment["DOTNET_NOLOGO"] = "1";
    psi.Environment["MSBUILDTERMINALLOGGER"] = "off";
    psi.Environment["NO_COLOR"] = "1";
    using var proc = new Process { StartInfo = psi };
    var sb = new StringBuilder();
    proc.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (sb) sb.AppendLine(e.Data); };
    proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (sb) sb.AppendLine(e.Data); };
    proc.Start();
    proc.BeginOutputReadLine();
    proc.BeginErrorReadLine();
    using var cts = new CancellationTokenSource(timeout);
    var timedOut = false;
    try { await proc.WaitForExitAsync(cts.Token); }
    catch (OperationCanceledException) { timedOut = true; try { proc.Kill(entireProcessTree: true); } catch { } }
    proc.WaitForExit();
    lock (sb) return new RunResult(timedOut ? 124 : proc.ExitCode, sb.ToString(), timedOut);
}

async Task<ImageResult> CheckImage(HttpClient http, string reference)
{
    // reference: mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled
    var noHost = reference["mcr.microsoft.com/".Length..];
    var repo = noHost[..noHost.IndexOf(':')];
    var tag = noHost[(noHost.IndexOf(':') + 1)..];
    const string accept = "application/vnd.oci.image.index.v1+json, application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.oci.image.manifest.v1+json, application/vnd.docker.distribution.manifest.v2+json";
    try
    {
        async Task<JsonDocument?> Get(string url)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Accept", accept);
            using var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            return JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        }
        var manifest = await Get($"https://mcr.microsoft.com/v2/{repo}/manifests/{tag}");
        if (manifest is null) return new ImageResult(reference, false, "-", false);
        if (manifest.RootElement.TryGetProperty("manifests", out var list))
        {
            var amd64 = list.EnumerateArray().FirstOrDefault(m => m.TryGetProperty("platform", out var pl) && pl.GetProperty("os").GetString() == "linux" && pl.GetProperty("architecture").GetString() == "amd64");
            if (amd64.ValueKind == JsonValueKind.Undefined) return new ImageResult(reference, true, "no linux/amd64 image", false);
            manifest = await Get($"https://mcr.microsoft.com/v2/{repo}/manifests/{amd64.GetProperty("digest").GetString()}");
            if (manifest is null) return new ImageResult(reference, true, "manifest unreadable", false);
        }
        var configDigest = manifest.RootElement.GetProperty("config").GetProperty("digest").GetString();
        var config = await Get($"https://mcr.microsoft.com/v2/{repo}/blobs/{configDigest}");
        var env = config?.RootElement.GetProperty("config").GetProperty("Env").EnumerateArray().Select(e => e.GetString() ?? "").ToList() ?? [];
        string? Env(string key) => env.FirstOrDefault(e => e.StartsWith(key + "="))?[(key.Length + 1)..];
        var version = Env("DOTNET_SDK_VERSION") ?? Env("ASPNET_VERSION") ?? Env("DOTNET_VERSION");
        // runtime-deps images carry no .NET version; resolving is all that can be checked.
        if (version is null) return new ImageResult(reference, true, "none (runtime-deps)", true);
        var released = version.StartsWith("11.0.") && !version.Contains('-');
        return new ImageResult(reference, true, version, released);
    }
    catch (Exception ex)
    {
        return new ImageResult(reference, false, ex.GetType().Name + ": " + ex.Message, false);
    }
}

static (int Passed, int Failed) ParseTests(string output)
{
    int passed = 0, failed = 0;
    // VSTest: "Passed!  - Failed:     0, Passed:    11, Skipped: ..."   MTP / terminal logger: "total: 11 failed: 0 succeeded: 11"
    foreach (Match m in Regex.Matches(output, @"Failed:\s*(\d+),\s*Passed:\s*(\d+)"))
    { failed += int.Parse(m.Groups[1].Value); passed += int.Parse(m.Groups[2].Value); }
    if (passed + failed == 0)
    {
        var f = Regex.Matches(output, @"failed:\s*(\d+)", RegexOptions.IgnoreCase);
        var s = Regex.Matches(output, @"succeeded:\s*(\d+)", RegexOptions.IgnoreCase);
        if (s.Count > 0) { passed = int.Parse(s[^1].Groups[1].Value); failed = f.Count > 0 ? int.Parse(f[^1].Groups[1].Value) : 0; }
    }
    return (passed, failed);
}

static string FirstErrors(string output)
{
    var lines = output.Split('\n').Select(l => l.Trim()).Where(l => Regex.IsMatch(l, @"\berror\b|\[FAIL\]|failed", RegexOptions.IgnoreCase)).Distinct().Take(2).ToList();
    var s = lines.Count > 0 ? string.Join(" | ", lines) : "non-zero exit code";
    return s.Length > 300 ? s[..300] + "..." : s;
}

IEnumerable<string> Find(string dir, string pattern) =>
    Directory.Exists(dir) ? Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories).Where(f => !IsBuildOutput(f) && Path.GetExtension(f) == pattern[1..]) : [];

static bool IsBuildOutput(string path) =>
    path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(s => s is "bin" or "obj" or ".git" or ".vs" or "node_modules");

static bool IsMauiHead(string proj)
{
    var t = File.ReadAllText(proj);
    return t.Contains("<UseMaui>true", StringComparison.OrdinalIgnoreCase) || t.Contains("net11.0-android");
}

// Blazor's ActivityInvestigations/ChapterNN/Tests projects reference ../LibraryDesk, which only
// exists once create_copy.py has copied them beside a checkpoint. The prepared ActivityStarts
// copies contain the same tests in place and are built and run instead.
static bool IsFragment(string proj)
{
    var dir = Path.GetDirectoryName(proj)!;
    return Regex.Matches(File.ReadAllText(proj), @"<ProjectReference\s+Include=""([^""]+)""")
        .Any(m => !File.Exists(Path.GetFullPath(Path.Combine(dir, m.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar)))));
}

static bool IsTest(string proj) =>
    Regex.IsMatch(File.ReadAllText(proj), @"Microsoft\.NET\.Test\.Sdk|""xunit|""bunit|MSTest|NUnit|<IsTestProject>true", RegexOptions.IgnoreCase);

static bool IsMtp(string proj) =>
    Regex.IsMatch(File.ReadAllText(proj), @"xunit\.v3\.mtp|TestingPlatform", RegexOptions.IgnoreCase);

static IEnumerable<string> SolutionProjects(string sln)
{
    var dir = Path.GetDirectoryName(sln)!;
    foreach (Match m in Regex.Matches(File.ReadAllText(sln), @"[""']([^""']+\.csproj)[""']"))
        yield return Path.GetFullPath(Path.Combine(dir, m.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar)));
}

static string? NearestGlobalJsonVersion(string dir)
{
    for (var d = new DirectoryInfo(dir); d is not null; d = d.Parent)
    {
        var gj = Path.Combine(d.FullName, "global.json");
        if (!File.Exists(gj)) continue;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(gj), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            return doc.RootElement.TryGetProperty("sdk", out var s) && s.TryGetProperty("version", out var v) ? v.GetString() : null;
        }
        catch { return null; }
    }
    return null;
}

string GroupKey(string path)
{
    // book/ChapterNN, book/Samples/Checkpoints/ChapterNN, book/FullExamples/ChapterNN, ...: the first
    // path segment named like ChapterNN, otherwise the project's own folder's parent.
    var parts = Rel(path).Split('/');
    for (var i = 0; i < parts.Length - 1; i++)
        if (Regex.IsMatch(parts[i], @"^Chapter\d+", RegexOptions.IgnoreCase)) return string.Join('/', parts[..(i + 1)]);
    return parts.Length > 2 ? string.Join('/', parts[..2]) : parts[0];
}

string Rel(string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
string BookOf(string path) => Rel(path).Split('/')[0];

static string Label(Status s) => s switch { Status.Pass => "PASS", Status.Fail => "FAIL", Status.Skip => "SKIP", _ => "XFAIL" };

static void PrintTable(List<string[]> rows)
{
    var widths = Enumerable.Range(0, rows[0].Length).Select(c => rows.Max(r => r[c].Length)).ToArray();
    string Line(string[] r) => "| " + string.Join(" | ", r.Select((v, c) => c == 0 ? v.PadRight(widths[c]) : v.PadLeft(widths[c]))) + " |";
    Console.WriteLine(Line(rows[0]));
    Console.WriteLine("|" + string.Join("|", widths.Select((w, c) => c == 0 ? new string('-', w + 2) : new string('-', w + 1) + ":")) + "|");
    foreach (var r in rows.Skip(1)) Console.WriteLine(Line(r));
}

enum Status { Pass, Fail, Skip, ExpectedFail }
record RunResult(int ExitCode, string Output, bool TimedOut);
record Result(string Kind, string Book, string Path, string FullPath, Status Status, string Detail, string Log, int Passed, int Failed, bool Retried);
record ImageResult(string Reference, bool Resolves, string Version, bool Pass);

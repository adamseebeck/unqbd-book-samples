# Chapter 8 delta: Authentication and Identity

Generated November 3, 2026 (ET) from the companion checkpoint `AdventureWorks/Chapter08` and the operations that rebuild it from the Chapter 7 checkpoint. Every snippet below is copied from the checkpoint files, and every output and response was captured from a run on the .NET 11 RC3 SDK (shown here with the release version numbers the book uses).

## What the chapter adds

Chapter 8 adds sign-in to the running app instead of a separate Chapter08 project. Everything from Chapters 1 to 7 stays.

- Packages: Microsoft.AspNetCore.Identity.EntityFrameworkCore and Microsoft.EntityFrameworkCore.Sqlite 11.0.0 in Infrastructure; Microsoft.AspNetCore.Authentication.JwtBearer 11.0.0 in Web.
- Infrastructure: `ApplicationUser : IdentityUser` with a stored `LoyaltyNumber` (the library-card example becomes store loyalty, design option A), `RefreshToken` (only a SHA-256 hash is stored; ConsumedByHash is a concurrency token) and the app's one database context, `AdventureWorksDbContext : IdentityDbContext<ApplicationUser>`, on the SQLite file `adventureworks.db` in the project folder.
- Web: `AccountServices.AddAccountServices` registers Identity (12-character passwords, unique email, lockout after 5 failures for 15 minutes), the Identity cookie (HttpOnly, Secure, SameSite Strict, /Account/Login, 2-hour sliding), security-stamp validation every 5 minutes, the named Bearer scheme, `TokenIssuer` and `LoyaltyClaimsTransformation`. Identity is the only cookie scheme, and its cookie events return 401 and 403 for `/api` paths instead of redirecting.
- `UseAuthentication` and `UseAuthorization` go between routing (with its endpoint logging and `/api` branch) and antiforgery.
- `/Account/Login` (PasswordSignInAsync with lockout, LocalRedirect) and `/Account/Profile` (pinned to Identity.Application).
- `AccountEndpoints`: `POST /api/auth/login` (password check with lockout, access and refresh tokens, no cookie), `POST /api/auth/refresh` (one-time rotation in one SaveChanges; a replay gets 401), `GET /api/account/me` (default cookie scheme) and the Bearer-only `GET /api/account/loyalty`, where a valid cookie alone gets 401.
- Development: `SeedDevelopmentDataAsync` runs EnsureCreated and creates five accounts with one password from Training:Password (User Secrets): casey (customer, AW-001), jordan (business buyer, Department Finance, AW-002), morgan (Finance staff), riley (born 2010, AW-003) and avery (no claims yet; Chapter 9 makes avery the Admin). Each has a DateOfBirth claim. `DevSignIn` signs a seeded account in by name with the real Identity cookie for requests.http and curl.exe; outside Development it maps nothing.
- The old "throw unless Development" is gone: the app starts in any environment. The signing key comes from Jwt:SigningKey; without one, each start uses a random key.

## Files added or changed

| File | Change |
| --- | --- |
| `AdventureWorks.Infrastructure/Identity/IdentityModels.cs` | added |
| `AdventureWorks/LoyaltyClaimsTransformation.cs` | added |
| `AdventureWorks/TokenIssuer.cs` | added |
| `AdventureWorks.Infrastructure/AdventureWorksDbContext.cs` | added |
| `AdventureWorks/appsettings.json` | replaced |
| `AdventureWorks/AccountServices.cs` | added |
| `AdventureWorks/Program.cs` | changed |
| `AdventureWorks/Pages/Account/Login.cshtml.cs` | added |
| `AdventureWorks/Pages/Account/Login.cshtml` | added |
| `AdventureWorks/Pages/Account/Profile.cshtml` | added |
| `AdventureWorks/AccountEndpoints.cs` | added |
| `AdventureWorks/DevelopmentSeed.cs` | added |
| `AdventureWorks/DevSignIn.cs` | added |
| (command) `dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore@11.0.0` | creates or edits project files |
| (command) `dotnet add package Microsoft.EntityFrameworkCore.Sqlite@11.0.0` | creates or edits project files |
| (command) `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer@11.0.0` | creates or edits project files |

## Steps, in order

Each step names the file and, for an existing file, the exact line the new code goes after or the block it replaces. Applying these steps to the previous checkpoint reproduces this checkpoint: the reader-path check found 56 of 56 files identical and no differences.

### Step 1: Add the packages

Identity's EF Core store and the SQLite provider go in Infrastructure, which owns persistence; the JWT Bearer handler goes in Web, which owns authentication. Run each command from the folder shown. In Visual Studio, use Manage NuGet Packages on each project.

Run this from the `AdventureWorks.Infrastructure` folder.  The package name makes this command longer than the print line; type it as one line.

```shell
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore@11.0.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.AspNetCore.Identity.EntityFrameworkCore' into project 'AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj'.
info : Restoring packages for AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.05.42.04/vulnerability.update.json
info : Package 'Microsoft.AspNetCore.Identity.EntityFrameworkCore' is compatible with all the specified frameworks in project 'AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj'.
info : PackageReference for package 'Microsoft.AspNetCore.Identity.EntityFrameworkCore' version '11.0.0' added to file 'AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj'.
info : Generating MSBuild file AdventureWorks.Infrastructure\obj\AdventureWorks.Infrastructure.csproj.nuget.g.props.
(3 more lines)
```

Run this from the `AdventureWorks.Infrastructure` folder.

```shell
dotnet add package Microsoft.EntityFrameworkCore.Sqlite@11.0.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.EntityFrameworkCore.Sqlite' into project 'AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj'.
info : Restoring packages for AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.05.42.04/vulnerability.update.json
info : Package 'Microsoft.EntityFrameworkCore.Sqlite' is compatible with all the specified frameworks in project 'AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj'.
info : PackageReference for package 'Microsoft.EntityFrameworkCore.Sqlite' version '11.0.0' added to file 'AdventureWorks.Infrastructure\AdventureWorks.Infrastructure.csproj'.
info : Generating MSBuild file AdventureWorks.Infrastructure\obj\AdventureWorks.Infrastructure.csproj.nuget.g.targets.
(2 more lines)
```

Run this from the `AdventureWorks` folder.  The package name makes this command longer than the print line; type it as one line.

```shell
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer@11.0.0
```

It prints:

```text
info : X.509 certificate chain validation will use the default trust store selected by .NET for code signing.
info : X.509 certificate chain validation will use the default trust store selected by .NET for timestamping.
info : Adding PackageReference for package 'Microsoft.AspNetCore.Authentication.JwtBearer' into project 'AdventureWorks\AdventureWorks.csproj'.
info : Restoring packages for AdventureWorks\AdventureWorks.csproj...
info :   CACHE https://api.nuget.org/v3/vulnerabilities/index.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/vulnerability.base.json
info :   CACHE https://api.nuget.org/v3-vulnerabilities/2026.10.28.05.41.06/2026.11.03.05.42.04/vulnerability.update.json
info : Package 'Microsoft.AspNetCore.Authentication.JwtBearer' is compatible with all the specified frameworks in project 'AdventureWorks\AdventureWorks.csproj'.
info : PackageReference for package 'Microsoft.AspNetCore.Authentication.JwtBearer' version '11.0.0' added to file 'AdventureWorks\AdventureWorks.csproj'.
info : Generating MSBuild file AdventureWorks\obj\AdventureWorks.csproj.nuget.g.props.
(3 more lines)
```

### Step 2: Define users, refresh tokens and the claims transformation

ApplicationUser and RefreshToken are persistence types that depend on Identity, so they live in Infrastructure, not Core. The loyalty number is stored on the user; the transformation turns it into a claim. The token issuer signs short-lived access tokens for a mobile client.

Create `AdventureWorks.Infrastructure/Identity/IdentityModels.cs` (28 lines).

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AdventureWorks.Infrastructure.Identity;

// A store customer or staff member. The loyalty number lives in
// the Identity store, never in a sign-in request.
public sealed class ApplicationUser : IdentityUser
{
    [MaxLength(20)]
    public string LoyaltyNumber { get; set; } = "";
}

// Only a SHA-256 hash of each refresh token is stored.
public sealed class RefreshToken
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string TokenHash { get; set; } = "";

    public string UserId { get; set; } = "";

    public DateTimeOffset Expires { get; set; }

    [ConcurrencyCheck, MaxLength(64)]
    public string? ConsumedByHash { get; set; }
}
```

Create `AdventureWorks/LoyaltyClaimsTransformation.cs` (32 lines).

```csharp
using System.Security.Claims;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace AdventureWorks;

// Adds the stored loyalty number as a claim. It can run more than
// once per request, so it skips a principal that already has one.
public sealed class LoyaltyClaimsTransformation(
    UserManager<ApplicationUser> users) : IClaimsTransformation
{
    public const string ClaimType = "loyalty_number";

    public async Task<ClaimsPrincipal> TransformAsync(
        ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true ||
            principal.HasClaim(c => c.Type == ClaimType))
            return principal;
        var id =
            principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null) return principal;
        var user = await users.FindByIdAsync(id);
        if (user is null || user.LoyaltyNumber.Length == 0)
            return principal;
        var clone = principal.Clone();
        clone.AddIdentity(new ClaimsIdentity(
            [new Claim(ClaimType, user.LoyaltyNumber)]));
        return clone;
    }
}
```

Create `AdventureWorks/TokenIssuer.cs` (48 lines). The signing key comes from configuration (Jwt:SigningKey, at least 32 characters, from User Secrets or a deployment secret store). Without one, every environment uses a random key per start, so the app never refuses to start and tokens simply end with the process.

TokenIssuer.cs, part 1 of 2 (23 lines):

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AdventureWorks;

// Signs short-lived access tokens for the mobile client. The key
// comes from Jwt:SigningKey (User Secrets locally, a secret store
// in deployment); without one, each start uses a random key, so
// tokens never outlive the process.
public sealed class TokenIssuer(TimeProvider clock,
    IConfiguration configuration)
{
    public const string Issuer = "AdventureWorks";
    public const string Audience = "AdventureWorks.Api";

    public SymmetricSecurityKey Key { get; } = new(
        configuration["Jwt:SigningKey"] is { Length: >= 32 } key
            ? Encoding.UTF8.GetBytes(key)
            : RandomNumberGenerator.GetBytes(32));
```

TokenIssuer.cs, part 2 of 2 (24 lines):

```csharp
    public string Issue(ApplicationUser user)
    {
        var now = clock.GetUtcNow();
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Id)
        ];
        var signing = new SigningCredentials(
            Key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience, claims,
            now.UtcDateTime, now.AddMinutes(15).UtcDateTime,
            signing);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string NewRefreshToken() =>
        Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
```

### Step 3: Define the app's database context

One context for the whole app, named for it: Chapter 10 adds the catalog and order tables to the same context. The connection string is a relative SQLite file, resolved against the project folder so `dotnet run` and Visual Studio use the same file.

Create `AdventureWorks.Infrastructure/AdventureWorksDbContext.cs` (22 lines).

```csharp
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure;

// One context for the whole app. Chapter 8 stores Identity users
// and refresh tokens; Chapter 10 adds the catalog and orders.
public sealed class AdventureWorksDbContext(
    DbContextOptions<AdventureWorksDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash).IsUnique();
    }
}
```

Replace the whole of `AdventureWorks/appsettings.json` with the following. Adds ConnectionStrings:AdventureWorks.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "AdventureWorks": "Data Source=adventureworks.db"
  },
  "FeatureFlags": {
    "EnableNewCheckout": true,
    "MaxCartItems": 20,
    "AllowedRegions": [ "US" ]
  }
}
```

### Step 4: Register Identity, the cookie and the Bearer handler

Identity is the only cookie scheme (Identity.Application). Its cookie events return 401 and 403 for /api paths instead of redirecting to the sign-in page, so every API in the app answers like an API. JWT Bearer is a second, named scheme that only the token API uses.

Create `AdventureWorks/AccountServices.cs` (93 lines).

AccountServices.cs, part 1 of 4 (11 lines):

```csharp
using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AdventureWorks;
```

AccountServices.cs, part 2 of 4 (33 lines):

```csharp
public static class AccountServices
{
    public static IServiceCollection AddAccountServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // A relative SQLite file lives in the project folder.
        var database = new SqliteConnectionStringBuilder(
            configuration.GetConnectionString("AdventureWorks"));
        database.DataSource = Path.Combine(
            environment.ContentRootPath, database.DataSource);
        services.AddDbContext<AdventureWorksDbContext>(options =>
            options.UseSqlite(database.ConnectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<TokenIssuer>();
        services.AddIdentity<ApplicationUser, IdentityRole>(
                ConfigureIdentity)
            .AddEntityFrameworkStores<AdventureWorksDbContext>()
            .AddDefaultTokenProviders();
        services.ConfigureApplicationCookie(ConfigureCookie);
        services.Configure<SecurityStampValidatorOptions>(o =>
            o.ValidationInterval = TimeSpan.FromMinutes(5));
        services.AddAuthentication()
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme);
        services.AddOptions<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme)
            .Configure<TokenIssuer>(ConfigureBearer);
        services.AddScoped<IClaimsTransformation,
            LoyaltyClaimsTransformation>();
        services.AddAuthorization();
        return services;
    }
```

AccountServices.cs, part 3 of 4 (24 lines):

```csharp
    static void ConfigureIdentity(IdentityOptions options)
    {
        options.Password.RequiredLength = 12;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    }

    static void ConfigureCookie(
        CookieAuthenticationOptions options)
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        // Pages redirect to sign in; APIs answer 401 or 403.
        options.Events.OnRedirectToLogin = context =>
            Answer(context, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context =>
            Answer(context, StatusCodes.Status403Forbidden);
    }
```

AccountServices.cs, part 4 of 4 (22 lines):

```csharp
    static Task Answer(
        RedirectContext<CookieAuthenticationOptions> context,
        int status)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = status;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    static void ConfigureBearer(
        JwtBearerOptions options, TokenIssuer issuer) =>
        options.TokenValidationParameters = new()
        {
            ValidIssuer = TokenIssuer.Issuer,
            ValidAudience = TokenIssuer.Audience,
            IssuerSigningKey = issuer.Key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `builder.Services.AddCatalog();`.

```csharp
builder.Services.AddAccountServices(
    builder.Configuration, builder.Environment);
```

In `AdventureWorks/Program.cs`, add these lines after the line `}));`. The anchor is the last line of the UseWhen branch; authentication and authorization go between routing and antiforgery, as the design's order says.

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

### Step 5: The login pages

The pages use the layout from Chapter 6. The profile page names the Identity cookie scheme explicitly.

Create `AdventureWorks/Pages/Account/Login.cshtml.cs` (44 lines).

Login.cshtml.cs, part 1 of 2 (22 lines):

```csharp
using System.ComponentModel.DataAnnotations;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdventureWorks.Pages.Account;

public sealed class LoginInput
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public sealed class LoginModel(
    SignInManager<ApplicationUser> signIn) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();
```

Login.cshtml.cs, part 2 of 2 (21 lines):

```csharp
    public async Task<IActionResult> OnPostAsync(
        string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();
        // The email doubles as the user name in this store.
        var result = await signIn.PasswordSignInAsync(Input.Email,
            Input.Password, isPersistent: false,
            lockoutOnFailure: true);
        if (result.Succeeded)
            return LocalRedirect(Url.IsLocalUrl(returnUrl)
                ? returnUrl! : "/Account/Profile");
        ModelState.AddModelError("", "Invalid login attempt.");
        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await signIn.SignOutAsync();
        return RedirectToPage("Login");
    }
}
```

Create `AdventureWorks/Pages/Account/Login.cshtml` (14 lines).

```razor
@page
@model AdventureWorks.Pages.Account.LoginModel
@{
    ViewData["Title"] = "Sign in";
}
<h1>Sign in</h1>
<form method="post">
    <div asp-validation-summary="All"></div>
    <label asp-for="Input.Email"></label>
    <input asp-for="Input.Email" />
    <label asp-for="Input.Password"></label>
    <input asp-for="Input.Password" />
    <button type="submit">Sign in</button>
</form>
```

Create `AdventureWorks/Pages/Account/Profile.cshtml` (15 lines).

```razor
@page
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize(
    AuthenticationSchemes = "Identity.Application")]
@{
    ViewData["Title"] = "Profile";
}
<h1>Profile</h1>
<p>Signed in as @User.Identity?.Name</p>
<p>Loyalty number: @User.FindFirst("loyalty_number")?.Value</p>
<p>Department: @User.FindFirst("Department")?.Value</p>
<form method="post" asp-page="/Account/Login"
      asp-page-handler="Logout">
    <button type="submit">Sign out</button>
</form>
```

### Step 6: The token API and the Bearer-only loyalty API

POST /api/auth/login checks the password with lockout and returns an access token and a refresh token; it issues no cookie. POST /api/auth/refresh consumes a refresh token once. /api/account/me uses the default (cookie) scheme; /api/account/loyalty names the Bearer scheme, so a valid cookie alone gets 401 there.

Create `AdventureWorks/AccountEndpoints.cs` (103 lines).

AccountEndpoints.cs, part 1 of 4 (30 lines):

```csharp
using System.Security.Claims;
using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks;

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string Token);

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", LogIn);
        app.MapPost("/api/auth/refresh", Refresh);
        // The signed-in user through the default (cookie) scheme.
        app.MapGet("/api/account/me", Me).RequireAuthorization();
        // Bearer only: a valid cookie alone gets 401 here.
        app.MapGet("/api/account/loyalty",
                (ClaimsPrincipal user) => Me(user))
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
        return app;
    }
```

AccountEndpoints.cs, part 2 of 4 (23 lines):

```csharp
    static async Task<IResult> LogIn(LoginRequest request,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        AdventureWorksDbContext db, TokenIssuer issuer,
        TimeProvider clock)
    {
        if (request.Email is not { Length: > 0 and <= 256 } ||
            request.Password is not { Length: > 0 and <= 1024 })
            return Results.Unauthorized();
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null) return Results.Unauthorized();
        var check = await signIn.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);
        if (!check.Succeeded) return Results.Unauthorized();
        var refresh = TokenIssuer.NewRefreshToken();
        db.RefreshTokens.Add(NewRow(refresh, user, clock));
        await db.SaveChangesAsync();
        return Results.Ok(new
        {
            AccessToken = issuer.Issue(user),
            RefreshToken = refresh
        });
    }
```

AccountEndpoints.cs, part 3 of 4 (30 lines):

```csharp
    static async Task<IResult> Refresh(RefreshRequest request,
        AdventureWorksDbContext db,
        UserManager<ApplicationUser> users, TokenIssuer issuer,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        if (request.Token is not { Length: > 0 and <= 256 })
            return Results.Unauthorized();
        var hash = TokenIssuer.Hash(request.Token);
        var old = await db.RefreshTokens.SingleOrDefaultAsync(
            t => t.TokenHash == hash, cancellationToken);
        if (old is null || old.ConsumedByHash is not null ||
            old.Expires <= clock.GetUtcNow())
            return Results.Unauthorized();
        var user = await users.FindByIdAsync(old.UserId);
        if (user is null || await users.IsLockedOutAsync(user))
            return Results.Unauthorized();
        var raw = TokenIssuer.NewRefreshToken();
        old.ConsumedByHash = TokenIssuer.Hash(raw);
        db.RefreshTokens.Add(NewRow(raw, user, clock));
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Unauthorized(); // a replay lost
        }
        return Results.Ok(new
        {
            AccessToken = issuer.Issue(user),
            RefreshToken = raw
        });
    }
```

AccountEndpoints.cs, part 4 of 4 (17 lines):

```csharp
    static RefreshToken NewRow(string raw, ApplicationUser user,
        TimeProvider clock) => new()
    {
        TokenHash = TokenIssuer.Hash(raw),
        UserId = user.Id,
        Expires = clock.GetUtcNow().AddDays(7)
    };

    static IResult Me(ClaimsPrincipal user) => Results.Ok(new
    {
        Name = user.Identity?.Name,
        Scheme = user.Identity?.AuthenticationType,
        LoyaltyNumbers = user.FindAll(
                LoyaltyClaimsTransformation.ClaimType)
            .Select(claim => claim.Value)
    });
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapRazorPages();`.

```csharp
app.MapAccountEndpoints();
```

### Step 7: Development accounts and the test sign-in

In Development the app creates its SQLite database and five test accounts: casey (customer), jordan (a business buyer in Finance), morgan (Finance staff), riley (born 2010) and avery (who becomes the Admin in Chapter 9). They share one password, read from Training:Password; without it the app logs a warning and creates no accounts. DevSignIn signs one in by name with the real Identity cookie, for requests.http and curl.exe; outside Development it maps nothing.

Create `AdventureWorks/DevelopmentSeed.cs` (76 lines).

DevelopmentSeed.cs, part 1 of 3 (25 lines):

```csharp
using System.Security.Claims;
using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace AdventureWorks;

public sealed record SeedAccount(string Name, string Loyalty,
    string Department, string DateOfBirth);

// Development only. EnsureCreated makes the local database;
// Chapter 10 explains EF Core and replaces this with migrations.
public static class DevelopmentSeed
{
    public const string Domain = "adventure-works.test";

    public static readonly SeedAccount[] Accounts =
    [
        // name, loyalty number, Department, date of birth
        new("casey", "AW-001", "", "1990-04-12"),     // customer
        new("jordan", "AW-002", "Finance", "1985-09-30"), // buyer
        new("morgan", "", "Finance", "1980-01-15"),   // staff
        new("riley", "AW-003", "", "2010-06-01"),     // under 21
        new("avery", "", "", "1975-11-20")            // admin
    ];
```

DevelopmentSeed.cs, part 2 of 3 (20 lines):

```csharp
    public static async Task SeedDevelopmentDataAsync(
        this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services
            .GetRequiredService<AdventureWorksDbContext>();
        await db.Database.EnsureCreatedAsync();
        var password = app.Configuration["Training:Password"];
        if (string.IsNullOrEmpty(password))
        {
            app.Logger.LogWarning(
                "Set Training:Password to create test accounts.");
            return;
        }
        var users = services
            .GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var account in Accounts)
            await CreateAsync(users, account, password);
    }
```

DevelopmentSeed.cs, part 3 of 3 (29 lines):

```csharp
    static async Task CreateAsync(
        UserManager<ApplicationUser> users,
        SeedAccount account, string password)
    {
        var email = $"{account.Name}@{Domain}";
        if (await users.FindByNameAsync(email) is not null)
            return;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LoyaltyNumber = account.Loyalty
        };
        Check(await users.CreateAsync(user, password));
        List<Claim> claims =
            [new("DateOfBirth", account.DateOfBirth)];
        if (account.Department.Length > 0)
            claims.Add(new("Department", account.Department));
        Check(await users.AddClaimsAsync(user, claims));
    }

    static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(
                "; ", result.Errors.Select(e => e.Description)));
    }
}
```

Create `AdventureWorks/DevSignIn.cs` (39 lines).

```csharp
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace AdventureWorks;

// Development only: signs in a seeded account by name with the
// real Identity cookie, so requests.http and curl.exe can act as
// a user. It reads no claims from the request. Outside
// Development it maps nothing.
public static class DevSignIn
{
    public static WebApplication MapDevelopmentSignIn(
        this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        var dev = app.MapGroup("/dev").WithTags("Development");
        dev.MapPost("/sign-in/{name}", SignInAs);
        dev.MapPost("/sign-out", SignOutCurrent);
        return app;
    }

    static async Task<IResult> SignInAs(string name,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn)
    {
        var email = $"{name}@{DevelopmentSeed.Domain}";
        var user = await users.FindByNameAsync(email);
        if (user is null) return Results.NotFound();
        await signIn.SignInAsync(user, isPersistent: false);
        return Results.Ok(new { user.UserName });
    }

    static async Task<IResult> SignOutCurrent(
        SignInManager<ApplicationUser> signIn)
    {
        await signIn.SignOutAsync();
        return Results.NoContent();
    }
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapDevelopmentEndpoints();`. Inside the Development-only block.

```csharp
    app.MapDevelopmentSignIn();
    await app.SeedDevelopmentDataAsync();
```

Run this from the `AdventureWorks` folder.  Any password with 12 or more characters, an uppercase and a lowercase letter, a digit and a symbol works; this is the companion's test value.

```shell
dotnet user-secrets set Training:Password "Practice-Pass-2026!"
```

It prints:

```text
Successfully saved Training:Password to the secret store.
```

## Program.cs at the checkpoint (73 lines)

Program.cs, part 1 of 3 (29 lines):

```csharp
using AdventureWorks;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddOrderServices(builder.Configuration);
builder.Services.AddCatalog();
builder.Services.AddAccountServices(
    builder.Configuration, builder.Environment);
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.Items["CorrelationId"]);
builder.Services.AddTransient<CorrelationIdMiddleware>();
builder.Services.AddTransient<DomainExceptionMiddleware>();
builder.Services.AddOpenApi();
builder.Services.AddControllers(options =>
    options.Filters.Add<RequestLoggingFilter>());
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();
builder.Services.AddLocalization();
builder.Services.AddValidation();
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = 1024 * 1024);
```

Program.cs, part 2 of 3 (18 lines):

```csharp
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseHttpsRedirection();
app.MapWhen(context => context.Request.Path == "/ping",
    ping => ping.Run(context =>
        context.Response.WriteAsync("pong")));
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<DomainExceptionMiddleware>();
app.UseStaticFiles();
app.UseRequestLocalization("en", "fr");
app.UseRouting();
app.UseMiddleware<EndpointLoggingMiddleware>();
```

Program.cs, part 3 of 3 (25 lines):

```csharp
app.UseWhen(context =>
    context.Request.Path.StartsWithSegments("/api"),
    api => api.Use(async (context, next) =>
    {
        context.Response.Headers["X-API-Pipeline"] = "true";
        await next();
    }));
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStatusEndpoints();
app.MapOrderEndpoints();
app.MapCatalogEndpoints();
app.MapControllers();
app.MapProductEndpointsV2();
app.MapRazorPages();
app.MapAccountEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDevelopmentEndpoints();
    app.MapDevelopmentSignIn();
    await app.SeedDevelopmentDataAsync();
}
app.Run();
```

## Commands the reader runs and what they print

Build from the working folder:

```shell
dotnet build
```

Result: exit 0, 0 warning(s), 0 error(s) (built with warnings as errors).

Run from the working folder:

```shell
dotnet run --project AdventureWorks
```

It prints (trimmed):

```text
Using launch settings from AdventureWorks\Properties\launchSettings.json...
Building...
```

In Visual Studio 2026, open `AdventureWorks.slnx` and press F5; the https profile starts the same app on https://localhost:7001. A headless Visual Studio 2026 rebuild of this checkpoint (devenv /rebuild Debug) rebuilt 3 project(s) with 0 warning(s) and 0 error(s).

## Requests and real responses

`requests.http` holds these requests in this order; 18 of 18 returned their commented status. The README has the same requests as curl.exe commands for Windows PowerShell 5.1, which also passed when run as printed.

- `POST /api/auth/login` returned **200**

  ```text
  {"accessToken":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjY4MDFlOTM2LWU2MTYtNDQ5Zi04NmY0LWU5NGJiZTViYWU0MyIsImh0dHA6Ly9zY2hlbWF...
  ```

- `POST /api/auth/login` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-e511bcaa0910b33638b352e404c3f937-65e063979842ea94-02","correlationId":"47cf158aa61e44a3b6941ba8e5bb166f"}
  ```

- `GET /api/account/loyalty` returned **200**

  ```text
  {"name":"casey@adventure-works.test","scheme":"AuthenticationTypes.Federation","loyaltyNumbers":["AW-001"]}
  ```

- `GET /api/account/loyalty` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-aa1981937b7cc37f07373d97f7ec2ce9-b29e0fb4a38978c0-02","correlationId":"f3130071ab314c0896acb601cc0fc71b"}
  ```

- `POST /api/auth/refresh` returned **200**

  ```text
  {"accessToken":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjY4MDFlOTM2LWU2MTYtNDQ5Zi04NmY0LWU5NGJiZTViYWU0MyIsImh0dHA6Ly9zY2hlbWF...
  ```

- `POST /api/auth/refresh` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-683ced461d47f737aaff23ae6da83a2b-0ee3e9bab67369a4-02","correlationId":"a47e2548e2a342548698c066a4690285"}
  ```

- `POST /api/auth/refresh` returned **200**

  ```text
  {"accessToken":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjY4MDFlOTM2LWU2MTYtNDQ5Zi04NmY0LWU5NGJiZTViYWU0MyIsImh0dHA6Ly9zY2hlbWF...
  ```

- `GET /api/account/me` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-73584b65db5748ba0235ccea6619851b-5963fb5dbe0c1e4c-02","correlationId":"e6301705672b469599629565bf4d8a57"}
  ```

- `GET /Account/Profile` returned **302** (Location: https://localhost:7001/Account/Login?ReturnUrl=%2FAccount%2FProfile)

- `GET /Account/Login` returned **200**

  ```text
  (an HTML page)
  ```

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /Account/Profile` returned **200**

  ```text
  (an HTML page)
  ```

- `GET /api/account/me` returned **200**

  ```text
  {"name":"casey@adventure-works.test","scheme":"Identity.Application","loyaltyNumbers":["AW-001"]}
  ```

- `GET /api/account/loyalty` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-cb4562b2ab7ac20a305735417645e62b-c7a8c0878076fded-02","correlationId":"376fcea91dc6467ab922ace6bd555838"}
  ```

- `POST /dev/sign-out` returned **204**

- `GET /api/account/me` returned **401**

  ```text
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Unauthorized","status":401,"traceId":"00-15d1e35f074d9e0e45e54c43d930e734-0750d5cfec008cfc-02","correlationId":"70d624850b0945a2b40b204be116449d"}
  ```

- `GET /api/catalog` returned **200**

  ```text
  {"items":[{"id":1,"name":"Trail Helmet","sku":"HLM-1001","price":29.99,"category":"accessories","version":1},{"id":2,"name":"Trail Jersey","sku":"JRS-2001","price":49.99,"category":"clothing","version":1},{"id":3,"name":...
  ```

- `GET /Products` returned **200**

  ```text
  (an HTML page)
  ```

### Other runs

- Production (`-- --environment Production`): the app starts, `/api/catalog` returns 200, `/dev/sign-in/casey` returns 404 (not mapped) and `/api/account/me` returns 401. Checked.
- Without Training:Password the app logs "Set Training:Password to create test accounts." and still starts (checked by the first run of the reader path, before the secret was set).

## A breakpoint for this chapter

Set a breakpoint in `AdventureWorks/LoyaltyClaimsTransformation.cs` on line 27:

```csharp
var clone = principal.Clone();
```

Press F5 (the https profile), then send POST /api/auth/login for casey, then GET /api/account/loyalty with the access token. When Visual Studio stops, the Locals window shows these values (recorded on RC3 by logging each one just above that line in a scratch copy; the runtime type is in brackets; Locals shows the same value in its Value column and the declared type in its Type column):

```text
principal.Identity!.AuthenticationType = AuthenticationTypes.Federation [String]
principal.Identity!.Name = casey@adventure-works.test [String]
id = 5a045396-8a0b-4e5a-9278-f9ca90c10fef [String]
user.LoyaltyNumber = AW-001 [String]
principal.Claims.Count() = 6 [Int32]
```

The Bearer handler has validated the token and built the principal from its claims; the transformation has looked up the user by the NameIdentifier claim. The stored loyalty number is about to be added as a new claim on a clone. Continue and the API returns it.

## What the Activity adds

The Activity needs no new production code; it exercises the checkpoint (sign in, inspect the cookie flags, call the Bearer API, rotate and replay a refresh token, change the stored loyalty number). The companion's copy adds `LoyaltyEditor.cs`, a Development-only `PUT /dev/loyalty/{name}` that changes the stored value the way a back-office tool would, so the last step can be scripted. Checked: signed in as casey, `/api/account/me` lists `["AW-001"]`; after the change, signing out and in again lists `["AW-901"]`, exactly once, because the transformation skips a principal that already has the claim and the claim is not stored in the cookie.

The concurrent-refresh step is scripted too: `RefreshRace.ps1` in the Activity folder signs in casey, then sends two refreshes of the same refresh token from one HttpClient before either answers, for five rounds. Run on RC3 in Windows PowerShell 5.1 (`.\RefreshRace.ps1` from the working folder, with the Activity app running), it printed:

```text
Round 1: 401 and 200
Round 2: 200 and 401
Round 3: 401 and 200
Round 4: 200 and 401
Round 5: 401 and 200
```

Every round took the [ConcurrencyCheck] path, not the "already consumed" check: the app's log shows both requests reading the token, then two updates of the same row, and the second changes nothing, so SaveChangesAsync throws DbUpdateConcurrencyException and the endpoint answers 401 (round 1, trimmed):

```text
Selected endpoint HTTP: POST /api/auth/refresh => Refresh
Selected endpoint HTTP: POST /api/auth/refresh => Refresh
SELECT ... FROM "RefreshTokens" AS "r" WHERE "r"."TokenHash" = @hash
SELECT ... FROM "RefreshTokens" AS "r" WHERE "r"."TokenHash" = @hash
UPDATE "RefreshTokens" SET "ConsumedByHash" = @p0
WHERE "Id" = @p1 AND "ConsumedByHash" IS NULL
INSERT INTO "RefreshTokens" (...) VALUES (...)
POST /api/auth/refresh elapsed 166.9428 ms
UPDATE "RefreshTokens" SET "ConsumedByHash" = @p0
WHERE "Id" = @p1 AND "ConsumedByHash" IS NULL
POST /api/auth/refresh elapsed 322.9177 ms
```

Across the five rounds the log has ten such UPDATE statements (two per round) and only five refresh-token INSERTs from the winners. EF Core logs no error for the losing save, so the 401 is the only visible sign. For comparison, two curl.exe calls started with Start-Job also answered 200 and 401 in each of three rounds, but the log had four UPDATEs for six refreshes: only one loser reached the concurrency check, and the other two failed at the earlier "already consumed" check, because each job starts its own PowerShell and the calls begin at slightly different times.

The reader-path check rebuilt `AdventureWorks/Chapter08-Activity` from the Chapter 8 checkpoint: no differences.

### Activity: change a stored claim value

The Activity needs no new code: it exercises the checkpoint. Work in a copy anyway, because it changes the database. Sign in as casey, read /api/account/me (loyalty number AW-001, listed once), change the stored value, sign in again and read it again (AW-901, still listed once). The companion's copy adds one Development-only route that changes the stored value, so the step can be scripted; in Visual Studio you can make the same change with SQL Server Object Explorer or the sqlite3 tool.

Create `AdventureWorks/LoyaltyEditor.cs` (31 lines).

```csharp
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace AdventureWorks;

public sealed record LoyaltyChange(string Number);

// Activity only, and Development only: changes a stored loyalty
// number the way a back-office tool would.
public static class LoyaltyEditor
{
    public static WebApplication MapLoyaltyEditor(
        this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        app.MapPut("/dev/loyalty/{name}", ChangeAsync);
        return app;
    }

    static async Task<IResult> ChangeAsync(string name,
        LoyaltyChange change, UserManager<ApplicationUser> users)
    {
        var email = $"{name}@{DevelopmentSeed.Domain}";
        var user = await users.FindByNameAsync(email);
        if (user is null) return Results.NotFound();
        user.LoyaltyNumber = change.Number;
        var result = await users.UpdateAsync(user);
        return result.Succeeded
            ? Results.NoContent() : Results.Problem();
    }
}
```

In `AdventureWorks/Program.cs`, add these lines after the line `app.MapDevelopmentSignIn();`.

```csharp
    app.MapLoyaltyEditor();
```

### Activity: two refreshes of one token at the same moment

The [ConcurrencyCheck] on ConsumedByHash is what stops a stolen refresh token from being used twice at once. A .http file sends one request at a time. Two curl.exe calls started with Start-Job do overlap, but each job starts its own PowerShell, so the second call often arrives after the first has saved and fails at the earlier "already consumed" check instead. This script sends both requests from one HttpClient before either answers, five rounds, each with a fresh sign-in. It is in the working folder, not in the app.

Create `RefreshRace.ps1` (36 lines).

```powershell
# Two refreshes of one refresh token, sent at the same moment.
# One rotates the token (200); the other loses (401): at the
# check for a consumed token if the first has already saved, or
# at the [ConcurrencyCheck] on ConsumedByHash if both read the
# token before either saved. The app's log tells them apart.
# Run it in Windows PowerShell 5.1 while the app is running:
#   .\RefreshRace.ps1
# If scripts are disabled on your machine, paste these lines
# into the PowerShell window instead.
param(
    [string]$Password = "Practice-Pass-2026!",
    [int]$Rounds = 5)

Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$auth = "https://localhost:7001/api/auth"

function Send([string]$path, [string]$json) {
    $content = New-Object System.Net.Http.StringContent(
        $json, [Text.Encoding]::UTF8, "application/json")
    $client.PostAsync("$auth/$path", $content)
}

$email = "casey@adventure-works.test"
foreach ($round in 1..$Rounds) {
    $login = (Send "login" ('{"email":"' + $email +
        '","password":"' + $Password + '"}')).Result
    $tokens = $login.Content.ReadAsStringAsync().Result |
        ConvertFrom-Json
    $body = '{"token":"' + $tokens.refreshToken + '"}'
    # Both requests start before either answers.
    $pair = @((Send "refresh" $body), (Send "refresh" $body))
    [Threading.Tasks.Task]::WaitAll($pair)
    $codes = $pair | ForEach-Object { [int]$_.Result.StatusCode }
    "Round {0}: {1}" -f $round, ($codes -join " and ")
}
```

Run it from the working folder in Windows PowerShell 5.1 while the app runs: `.\RefreshRace.ps1`. If scripts are disabled on the machine, paste its lines into the window instead. The output and the app's log are under "What the Activity adds" above.

### Activity requests and responses

25 of 25 requests in `Chapter08-Activity/requests.http` returned their commented status. The responses for the requests the Activity adds:

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/account/me` returned **200**

  ```text
  {"name":"casey@adventure-works.test","scheme":"Identity.Application","loyaltyNumbers":["AW-001"]}
  ```

- `PUT /dev/loyalty/casey` returned **204**

- `POST /dev/sign-out` returned **204**

- `POST /dev/sign-in/casey` returned **200**

  ```text
  {"userName":"casey@adventure-works.test"}
  ```

- `GET /api/account/me` returned **200**

  ```text
  {"name":"casey@adventure-works.test","scheme":"Identity.Application","loyaltyNumbers":["AW-901"]}
  ```

- `PUT /dev/loyalty/casey` returned **204**

## The chapter's current listings: replaced, kept or removed

| Current listing | Status | What changes |
| --- | --- | --- |
| Step 1 Chapter08.csproj | Replaced | Three `dotnet add package` commands (two in Infrastructure, one in Web); the project files are tool output and are not printed. Two of the commands exceed 66 characters because of the package names, so they carry the "type it as one line" note. |
| Step 2 SecurityModels.cs (64 lines) | Replaced | Split by layer: Infrastructure/Identity/IdentityModels.cs (ApplicationUser with LoyaltyNumber, RefreshToken), Web's TokenIssuer.cs (key from configuration) and LoyaltyClaimsTransformation.cs ("loyalty_number"). LoginRequest and RefreshRequest move to AccountEndpoints.cs. |
| Step 3 ApplicationDbContext.cs | Replaced | AdventureWorksDbContext in Infrastructure, the app's one context. |
| Step 4 Pages/_ViewImports.cshtml | Removed | Chapter 6's file already registers the tag helpers. |
| Step 4 Login.cshtml.cs, Login.cshtml, Profile.cshtml | Replaced | Use the layout; the profile shows the loyalty number and Department claims. |
| Step 5 text | Replaced | No "refuses to start outside Development"; the password comes from User Secrets (Training:Password) instead of an environment variable. |
| Full Example Program.cs (103 lines) | Replaced | Adds 7 lines to Chapter 7's (AddAccountServices, UseAuthentication, UseAuthorization, MapAccountEndpoints, and DevSignIn plus the seed in the Development block); 73 lines. The registrations live in AccountServices.cs and the endpoints in AccountEndpoints.cs. |
| Full Example repeats of the model, context and page files | Removed | Printed once. |

## Notes for the writers

- Program.cs is 73 lines at this checkpoint, over the design's "about 60". The pipeline lines (Chapters 1 to 4 and now authentication) stay visible in Program.cs on purpose; the services and endpoints are one line per feature.
- The Bearer API is `/api/account/loyalty` (design, inventory option A). Quiz question 2 ("Bearer-only books endpoint") and question 4 (`library_card`) need the loyalty names.
- `/api/account/me` is new: a cookie-scheme API that shows the design's rule that APIs answer 401 instead of redirecting, and gives the Activity a place to read the claim.
- Users are created with UserName equal to their email (casey@adventure-works.test), so the login page and the token API both accept the email.
- The refresh-token replay answers 401 without the concurrency exception path being needed in a sequential run; the `[ConcurrencyCheck]` path covers two simultaneous refreshes, as the chapter explains.
- `adventureworks.db` is created in the AdventureWorks project folder (the connection string's relative path is resolved against the content root, so `dotnet run --project` and Visual Studio F5 use the same file). Chapter 10 tells the reader to delete it before the first migration.
- The test password `Practice-Pass-2026!` is companion test data only.

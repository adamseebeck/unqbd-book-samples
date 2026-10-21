from pathlib import Path
from hashlib import sha256
import argparse, json, shutil

parser = argparse.ArgumentParser(description="Create a disposable activity investigation copy; never edit the source package.")
parser.add_argument("--chapter", type=int, required=True)
parser.add_argument("--output", type=Path, required=True)
args = parser.parse_args()
samples = Path(__file__).resolve().parent.parent
spec = json.loads((Path(__file__).parent / f"Chapter{args.chapter:02}" / "delta.json").read_text(encoding="utf-8"))
output = args.output.resolve()
if output == samples or samples in output.parents or output.exists():
    raise SystemExit("Choose a new output directory outside the source Samples tree.")
output.mkdir(parents=True)
if args.chapter in [18, 20, 23]:
    names = ["LibraryDesk.Pwa"] if args.chapter == 20 else ["LibraryDesk", "LibraryDesk.Tests"] if args.chapter == 18 else ["LibraryDesk"]
    for name in names:
        shutil.copytree(samples / name, output / name, ignore=shutil.ignore_patterns("bin", "obj", ".git"))
    shutil.copy2(samples / "global.json", output / "global.json")
else:
    shutil.copytree(samples / "Checkpoints" / f"Chapter{args.chapter:02}", output, dirs_exist_ok=True, ignore=shutil.ignore_patterns("bin", "obj", ".git"))
records = []
verified = set()
for delta in spec["deltas"]:
    target = output / delta["file"]
    original = target.read_text(encoding="utf-8")
    before = sha256(target.read_bytes()).hexdigest()
    if delta["file"] not in verified and before != delta["source_sha256"]:
        raise SystemExit("Baseline hash differs: " + delta["file"])
    verified.add(delta["file"])
    # Multiple ordered edits can target one file; each expected text must occur exactly once.
    if original.count(delta["before"]) != 1:
        raise SystemExit("Target does not match the documented baseline: " + delta["file"])
    target.write_text(original.replace(delta["before"], delta["after"], 1), encoding="utf-8")
    records.append({"file": delta["file"], "before_sha256": before, "after_sha256": sha256(target.read_bytes()).hexdigest()})
(output / "investigation-manifest.json").write_text(json.dumps({"chapter": args.chapter, "changes": records, "expected": spec["expected"]}, indent=2), encoding="utf-8")
tests = Path(__file__).parent / f"Chapter{args.chapter:02}" / "Tests"
if tests.exists(): shutil.copytree(tests, output / "Tests")
print(json.dumps({"output": str(output), "chapter": args.chapter, "changed_files": records}, indent=2))

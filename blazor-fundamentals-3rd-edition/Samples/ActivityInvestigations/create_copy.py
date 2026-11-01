"""Make a copy of a chapter's checkpoint for that chapter's activity.

Usage, from the Samples/ActivityInvestigations folder:

    python create_copy.py --chapter 6 --output C:/BookExercises/Chapter06

The script copies Samples/Checkpoints/ChapterNN to the new folder, checks that
each file it changes is still the one the activity was written for, and applies
the changes in ChapterNN/delta.json. It also copies the activity's README.md,
and its Tests folder when the activity has one. The companion code itself is
never changed.
"""
from pathlib import Path
from hashlib import sha256
import argparse
import json
import shutil


def normalized_hash(data: bytes) -> str:
    # Hash with LF line endings, so a Windows (CRLF) or macOS/Linux (LF)
    # copy of the companion code gives the same result.
    return sha256(data.replace(b"\r\n", b"\n")).hexdigest()


parser = argparse.ArgumentParser(
    description="Copy a chapter's checkpoint and apply its activity change.")
parser.add_argument("--chapter", type=int, required=True,
                    help="chapter number, such as 6")
parser.add_argument("--output", type=Path, required=True,
                    help="a new folder outside the companion code")
args = parser.parse_args()

here = Path(__file__).resolve().parent
samples = here.parent
activity = here / f"Chapter{args.chapter:02}"
checkpoint = samples / "Checkpoints" / f"Chapter{args.chapter:02}"
if not (activity / "delta.json").is_file() or not checkpoint.is_dir():
    raise SystemExit(f"Chapter {args.chapter} has no activity copy. "
                     "See Samples/Activities for its answer.")
spec = json.loads((activity / "delta.json").read_text(encoding="utf-8"))

output = args.output.expanduser().resolve()
if output == samples or samples in output.parents:
    raise SystemExit("Choose an output folder outside the companion code.")
if output.exists():
    raise SystemExit(f"{output} already exists. Choose a new folder.")

shutil.copytree(checkpoint, output,
                ignore=shutil.ignore_patterns("bin", "obj", ".git", ".vs"))
(output / "README.md").unlink(missing_ok=True)  # the checkpoint's README

records = []
checked = set()
try:
    for delta in spec["deltas"]:
        target = output / delta["file"]
        raw = target.read_bytes()
        if delta["file"] not in checked:
            if normalized_hash(raw) != delta["source_sha256"]:
                raise SystemExit(
                    f"{delta['file']} in Checkpoints/Chapter{args.chapter:02}"
                    " is not the file this activity was written for. "
                    "Download the companion code again.")
            checked.add(delta["file"])
        text = raw.decode("utf-8")
        crlf = "\r\n" in text
        text = text.replace("\r\n", "\n")
        # Several changes can target one file, in order; each text that
        # is replaced must occur exactly once.
        if text.count(delta["before"]) != 1:
            raise SystemExit(f"{delta['file']} does not contain the text "
                             "this activity changes.")
        text = text.replace(delta["before"], delta["after"], 1)
        if crlf:
            text = text.replace("\n", "\r\n")
        target.write_bytes(text.encode("utf-8"))
        records.append({"file": delta["file"],
                        "before_sha256": sha256(raw).hexdigest(),
                        "after_sha256": sha256(target.read_bytes()).hexdigest()})
except BaseException:
    shutil.rmtree(output, ignore_errors=True)
    raise

shutil.copy2(activity / "README.md", output / "README.md")
if (activity / "Tests").is_dir():
    shutil.copytree(activity / "Tests", output / "Tests",
                    ignore=shutil.ignore_patterns("bin", "obj"))
manifest = {"chapter": args.chapter, "changes": records,
            "expected": spec["expected"]}
(output / "investigation-manifest.json").write_text(
    json.dumps(manifest, indent=2), encoding="utf-8")

print(f"Created {output}")
for name in dict.fromkeys(record["file"] for record in records):
    print(f"  changed {name}")
if (output / "Tests").is_dir():
    print("  added Tests (see README.md)")

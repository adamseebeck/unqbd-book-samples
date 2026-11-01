# Optional activity generator

The primary exercise route is Samples/ActivityStarts: copy the named folder and open LibraryDesk/LibraryDesk.slnx in Visual Studio. No Python command is needed for that route.

create_copy.py is an optional alternative for Chapters 2 to 23 except Chapter 4. It requires Python 3 and a new output folder outside the companion download. From this folder:

```powershell
python --version
python create_copy.py --chapter 6 --output C:/BookExercises/Chapter06
```

The script checks each changed file's normalized hash, copies the checkpoint, applies the exercise changes, adds any exercise test project to LibraryDesk.slnx and writes an investigation-manifest.json receipt. It does not solve the task. Open the output's solution in Visual Studio and read its README. Chapter 1 is a text activity; Chapter 4's prepared start is a copy of its unchanged checkpoint.

All reader global.json files request released SDK 11.0.100 with latestFeature. Chapter 2's activity deliberately starts with 11.0.999, the broken SDK selection the reader must repair. Do not change the other pins to a release candidate.

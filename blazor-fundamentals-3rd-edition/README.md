# Blazor Fundamentals, Third Edition: companion code

Companion code for *Blazor Fundamentals, Third Edition* by Adam Seebeck: the LibraryDesk application, built up across 23 chapters with .NET 11. The code is in the Samples folder; paths in the book such as Samples/Checkpoints/Chapter03 refer to it.

## Prerequisites

- The .NET 11 SDK, version 11.0.100 or later. Each global.json selects it and accepts later .NET 11 feature bands; the framework packages use version 11.0.0.
- Python 3. It runs the activity copy script and, in Chapters 20 and 23, serves the published offline app.
- A current browser such as Microsoft Edge or Google Chrome.
- A code editor if you like: Visual Studio (a version that supports .NET 11) and Visual Studio Code with the C# Dev Kit extension both work. The command line is the route the book describes.

## What is in Samples

- Samples/Checkpoints/Chapter02 to Chapter23: the LibraryDesk project at the end of each chapter, laid out like your working folder, each with a README.
- Samples/LibraryDesk: the finished app (the solution folder with the server and client projects). Samples/LibraryDesk.Tests holds its component tests, Samples/LibraryDesk.Pwa is Chapter 20's offline app, and Samples/LibraryDesk.Pwa.Tests holds the offline app's storage tests.
- Samples/Activities: the answer to every chapter's activity.
- Samples/ActivityInvestigations: create_copy.py and each activity's change. It makes a copy of a chapter's checkpoint with the change applied.
- Samples/ActivitySolutions: solved copies for the activities of Chapters 4, 5 and 13.

See Samples/README.md for the folder map and more detail.

## Follow along and catch up

Work through the chapters in order, in one working folder. Chapter 2 creates it, and every later chapter adds to the same project. If you skip a chapter, or your project stops working and you can't find why, copy the previous chapter's checkpoint folder, Samples/Checkpoints/ChapterNN, to a new working folder and continue there.

## Run and test

From the working folder, run the app, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

The terminal shows Now listening on: http://127.0.0.1:58631, and the app is ready in your browser at that address. Press Ctrl+C to stop it. From Chapter 18 on, run the component tests with the app stopped:

```powershell
dotnet test LibraryDesk.Tests
```

For an activity, go to Samples/ActivityInvestigations and run python create_copy.py --chapter 6 --output C:/BookExercises/Chapter06, replacing 6 with the chapter number and choosing a new folder. The README in the copy gives the command to run and the result to expect.

Found a problem in the code? Contact us at [unqbd.com/Contact](https://www.unqbd.com/Contact).

# Samples

Companion code for *Blazor Fundamentals, Third Edition*. Paths in the book such as Samples/Checkpoints/Chapter03 start in this folder.

## What you need

- The .NET 11 SDK, version 11.0.100 or later. Each global.json selects it, and the framework packages use the matching framework version: 11.0.0 with SDK 11.0.100, or a later 11.0 patch, such as 11.0.1, with a later SDK. This code uses 11.0.0.
- Python 3, for ActivityInvestigations/create_copy.py and, in Chapters 20 and 23, to serve the published offline app. Install it from python.org, and select Add python.exe to PATH if the installer offers it. In a new terminal, python --version should print Python 3 and a version number; if Windows opens the Microsoft Store or says Python was not found, use py in place of python.
- A current browser such as Microsoft Edge or Google Chrome.

## What is here

```text
Samples/
    Checkpoints/            your project at the end of each chapter,
                            Chapter02 to Chapter23
    LibraryDesk/            the finished app (solution folder)
    LibraryDesk.Tests/      its component tests (Chapters 18 to 21)
    LibraryDesk.Pwa/        the offline app (Chapter 20)
    LibraryDesk.Pwa.Tests/  storage tests for the offline app
    Activities/             each activity's answer
    ActivityInvestigations/ copy script and activity changes
    ActivitySolutions/      solved copies (Chapters 4, 5, 11 and 13)
    global.json             the SDK pin
```

## Follow along

The book builds one LibraryDesk project, in one working folder, from Chapter 2 to Chapter 23. Each chapter starts where the previous one ended and adds a page or a feature. Checkpoints/ChapterNN is that project at the end of chapter NN, laid out like your working folder: global.json and the LibraryDesk solution folder (LibraryDesk.sln, the LibraryDesk server project and the LibraryDesk.Client project), plus LibraryDesk.Tests from Chapter 18 and LibraryDesk.Pwa from Chapter 20. Each checkpoint has a README with its run command and the pages its chapter added.

The finished app in this folder, LibraryDesk with LibraryDesk.Tests and LibraryDesk.Pwa, is the same as Checkpoints/Chapter23.

## Catch up

If you skip a chapter, or your project stops working and you can't find why, copy the previous chapter's checkpoint folder to a new working folder, such as C:/BookExercises/LibraryDesk, and continue there. Work in the copy, so bin, obj and publish folders stay out of the companion code.

## Run and test

From the working folder, run the app with this command, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

The terminal shows Now listening on: http://127.0.0.1:58631. Open that address; press Ctrl+C in the terminal to stop the app.

From Chapter 18 on, stop the app and run the component tests from the same folder:

```powershell
dotnet test LibraryDesk.Tests
```

For the finished app, near the end the output reads Test summary: total: 24, failed: 0, succeeded: 24, skipped: 0, followed by the duration, and the last line starts with Build succeeded. The offline app has its own tests: dotnet test LibraryDesk.Pwa.Tests reads Test summary: total: 2, failed: 0, succeeded: 2, skipped: 0, and the last line starts with Build succeeded.

## Activities

Do each activity in a copy, so your working project stays as it is. From the ActivityInvestigations folder, run:

```powershell
python create_copy.py --chapter 6 --output C:/BookExercises/Chapter06
```

Replace 6 with the chapter number and choose a new folder. The script copies that chapter's checkpoint and sets up the activity: some copies arrive with a change that creates the situation you investigate, some with a Tests folder, and many unchanged, because you write the code yourself. It puts the activity's README in the copy, with what to change, the command to run and the result to expect. Chapters 1 and 4 have no copy script: Chapter 1's activity is written in a text file, and Chapter 4's is done in a copy of Checkpoints/Chapter04. ActivitySolutions holds solved copies for Chapters 4, 5, 11 and 13; those for Chapters 4, 5 and 13 have a test in their Tests folder. You can build and test an ActivitySolutions folder where it is; dotnet adds bin and obj folders there, which you can delete at any time. Activities/ChapterNN.md explains every activity's answer.

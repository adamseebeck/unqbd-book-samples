# C# Fundamentals ninth edition: examples

For students working through the book. The examples use C# 15 and .NET 11. Install the current .NET 11 SDK (version 11.0.100 or later) from the [official .NET 11 download page](https://dotnet.microsoft.com/en-us/download/dotnet/11.0). The included global.json files select it.

You can read and copy any file here in your browser. To run the examples, clone this repository or download it as a ZIP (the green Code button on GitHub), then open a terminal in this `csharp-fundamentals-9th-edition` folder and check `dotnet --version`. Package restore uses public NuGet packages and needs an internet connection the first time. Each lesson is independent; replace the whole Program.cs when the book says to use a Full Example, rather than appending another top-level program.

## Visual Studio on Windows

Use Visual Studio 2026 with the .NET desktop development workload and the released .NET 11 SDK. Choose File > Open > Project/Solution and select a chapter's ChapterNN.slnx or .csproj. For example, FullExamples/Chapter03/Chapter03.slnx loads the greeting project. In Solution Explorer open Program.cs. Build with Ctrl+Shift+B, run without the debugger with Ctrl+F5, or use F9 and F5 to set a breakpoint and start debugging. See Chapters 3-8 and 16 for guided investigations.

For multi-project chapters, open the solution to load the library and tests together. FullExamples/Chapter19/Chapter19.slnx opens SimpleCalculator and SimpleCalculator.Tests. Build, choose Test > Test Explorer, and run all tests. Right-click a test and choose Debug to inspect the actual library. StudentTracker/Chapter19/Chapter19.slnx similarly includes the tracker and its tests. If a solution has several executable projects, right-click the intended application in Solution Explorer and choose Set as Startup Project before F5/Ctrl+F5.

## VS Code alternative

Install Microsoft's C# Dev Kit and use File > Open Folder on the chapter folder or on this sample root. Load the chapter solution/project if prompted. The terminal commands below run the same projects. For debugging choose Run > Start Debugging and the intended C# project, then use VARIABLES, WATCH, CALL STACK, and the stepping toolbar. Set csharp.debug.console to integratedTerminal for programs that ask for console input.

The Debugging folder contains separate practice projects for Chapters 4, 6, 7, 8, and 16. Read their README files for the intentional defect and the expected observations. The book's Chapter 3 greeting and Chapter 19 tests provide the first and final debugger lessons.

## Run a complete lesson

```powershell
dotnet run --project FullExamples/Chapter03/Chapter03.csproj
```

Follow the prompts using the book's sample inputs. Chapters 3-18, 20-21 and 23-26 follow the ChapterNN.csproj naming pattern. Chapter 22 uses `FullExamples/Chapter22/SchoolDemo.csproj`. Chapters 1-2 are setup exercises; their template commands are in the book. Chapter 19 is a library with a separate test project.

## Work on an activity

Try the activity before reading its solution. For example:

```powershell
dotnet run --project ActivitySolutions/Chapter03/Chapter03Activity.csproj
```

The single-file `ActivitySolutions/ChapterNN.cs` copies match the book's solution links. Their ChapterNN folders contain runnable copies and project files. Chapters 19 and 22 use multiple files instead; read their local README files. File-writing activities use the terminal's working directory or a documented temporary directory. Run each exercise in a writable practice folder.

## Run Chapter 19 tests

From this folder:

```powershell
cd FullExamples/Chapter19
dotnet test --project SimpleCalculator.Tests
```

The local global.json selects Microsoft.Testing.Platform. The test project uses xunit.v3.mtp-v2 4.0.1 and references the actual SimpleCalculator library. Its 11 tests should pass. Return to this folder before using other commands from this page.

## Student Tracker project

The book builds one program, the Student Tracker, a step at a time from Chapter 3 to Chapter 22. `StudentTracker/ChapterNN` holds the program as it stands at the end of that chapter (Chapters 12, 17 and 18 add nothing to it), so you can compare your copy or pick up from any chapter. From this folder:

```powershell
dotnet run --project StudentTracker/Chapter05/StudentTracker
```

From Chapter 19, each folder also has the tests:

```powershell
dotnet run --project StudentTracker/Chapter19/StudentTracker.Tests
```

Chapters 20 to 22 save students.json or students.db in the process working directory: normally the executable's bin/Debug/net11.0 folder for Visual Studio launches, or the terminal's current folder for dotnet run; delete that file to start with an empty class. Chapter 22 uses Microsoft.EntityFrameworkCore.Sqlite 11.0.0 and needs an internet connection the first time.

## Arguments and Saved Files

Chapter 17's activity has Chapter17Activity (normal), Deterministic, Fail, and Cancel profiles. Chapter 20's activity has Chapter20Activity (normal), Missing, and Corrupt profiles. In Visual Studio select the intended profile beside Start before Ctrl+F5/F5. In VS Code C# Dev Kit the profiles appear in its C# launch selection. Terminal arguments printed in the book remain equivalent; the normal profile has no extra arguments. Stop before changing a profile and return to the normal profile afterward. Relative files are created in the process working directory, so use the same launcher when checking saved StudentTracker data.

## Package contents

There are 48 lesson/activity groups containing 51 projects, including libraries, tests and the separate migration experiment, plus the Student Tracker's 17 chapter versions (21 projects). Five additional Debugging projects provide separate inspection exercises. Chapter solutions group the relevant projects for the IDE. This folder contains source and configuration files, with build outputs, generated databases and the SDK excluded. These examples are companions to the book; intermediate snippets and intentional compiler-error demonstrations remain in the chapters.

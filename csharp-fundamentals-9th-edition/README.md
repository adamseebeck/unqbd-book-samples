# C# Fundamentals ninth edition: examples

For students working through the book. The examples use C# 15 and .NET 11. Install the current .NET 11 SDK (version 11.0.100 or later) from the [official .NET 11 download page](https://dotnet.microsoft.com/en-us/download/dotnet/11.0). The included global.json files select it.

You can read and copy any file here in your browser. To run the examples, clone this repository or download it as a ZIP (the green Code button on GitHub), then open a terminal in this `csharp-fundamentals-9th-edition` folder and check `dotnet --version`. Package restore uses public NuGet packages and needs an internet connection the first time. Each lesson is independent; replace the whole Program.cs when the book says to use a Full Example, rather than appending another top-level program.

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

The book builds one program, the Student Tracker, a step at a time from Chapter 3 to Chapter 22. `StudentTracker/ChapterNN` holds the program as it stands at the end of that chapter (Chapters 12, 16 and 17 add nothing to it), so you can compare your copy or pick up from any chapter. From this folder:

```powershell
dotnet run --project StudentTracker/Chapter05/StudentTracker
```

From Chapter 19, each folder also has the tests:

```powershell
dotnet run --project StudentTracker/Chapter19/StudentTracker.Tests
```

Chapters 20 to 22 save students.json or students.db in the folder you run from; delete that file to start with an empty class. Chapter 22 uses Microsoft.EntityFrameworkCore.Sqlite 11.0.0 and needs an internet connection the first time.

## Package contents

There are 48 lesson/activity groups containing 51 projects, including libraries, tests and the separate migration experiment, plus the Student Tracker's 17 chapter versions (21 projects). This folder contains source and configuration files, with build outputs, generated databases and the SDK excluded. These examples are companions to the book; intermediate snippets and intentional compiler-error demonstrations remain in the chapters.

# ASP.NET Core Fundamentals, Second Edition: companion code

Code for *ASP.NET Core Fundamentals: Building Modern Web Applications with .NET 11* by Adam Seebeck (unQbd).

The book builds one app, the AdventureWorks outdoor-gear store, from Chapter 1 to Chapter 18. The `AdventureWorks` folder holds it at the end of every chapter: `AdventureWorks/ChapterNN` is the app after Chapter NN, the folder the next chapter starts from. Each chapter also has a worked solution to its Activity (`ChapterNN-Activity`), and Chapter 15 has a debugging lab (`Chapter15-DebugLab`). Each folder builds and runs on its own and has a README with its steps and its requests.

You can read and copy any file here in your browser. To run the code, clone this repository or download it as a ZIP (the green Code button on GitHub).

## What you need

- The .NET 11 SDK, version 11.0.100 or later, from the [.NET 11 download page](https://dotnet.microsoft.com/download/dotnet/11.0). Each folder's `global.json` selects it.
- Visual Studio 2026, or VS Code with the C# Dev Kit extension. Every step also works from a terminal.
- Chapter 10's Activity only: SQL Server LocalDB, which Visual Studio 2026 installs on Windows.
- Chapter 18 only: Docker, or another engine that runs Linux containers.

## Where to start

Start with `AdventureWorks/Chapter01`. Open a terminal in that folder, the one that holds `global.json`, and run every command from there:

```shell
dotnet --version
dotnet run --project AdventureWorks
```

`dotnet --version` should print 11.0.100 or a later 11.0 version. The app listens on https://localhost:7001 and http://localhost:5001; if the browser warns about the certificate, run `dotnet dev-certs https --trust` once. In Visual Studio 2026, open the folder's `AdventureWorks.slnx` (from Chapter 2) and press F5.

To start a later chapter, open the previous chapter's folder: Chapter 7 starts from `AdventureWorks/Chapter06`. [AdventureWorks/README.md](AdventureWorks/README.md) lists what each chapter adds, the projects in the solution, and the extra step some chapters need, such as a User Secret or the warehouse service.

## Sending requests

Every chapter folder has a `requests.http` file with its requests in order and the status you should see. In Visual Studio 2026, open it and select **Send Request**; in VS Code, install the REST Client extension first. Each README has the same requests as curl.exe commands for Windows PowerShell 5.1.

## About the book

The book explains every step and the decisions behind it. More programming books are at [unqbd.com](https://www.unqbd.com).

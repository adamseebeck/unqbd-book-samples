# ASP.NET Core Fundamentals, Second Edition: companion code

Code for *ASP.NET Core Fundamentals: Building Modern Web Applications with .NET 11* by Adam Seebeck (unQbd). Each chapter folder holds that chapter's complete example, plus the worked activity solutions the chapters mention.

You can read and copy any file here in your browser. To run the projects, clone this repository or download it as a ZIP (the green Code button on GitHub).

## What you need

- The .NET 11 SDK, version 11.0.100 or later, from the [official .NET 11 download page](https://dotnet.microsoft.com/download/dotnet/11.0). The `global.json` files select it.
- Projects target `net11.0` and list their exact package versions. Microsoft framework packages use 11.0.0; other libraries follow their own version numbers.
- A few exercises need more: an isolated SQL Server instance for Chapter 10's SQL Server steps, a Redis-compatible server for Chapter 12's shared cache, a Linux container engine for Chapter 18's images, a native compiler for Native AOT, and a browser passkey authenticator only if you try passkeys beyond Chapter 8's password example. The chapter tells you when.

## Run an example

```shell
cd Chapter01/FullExample
dotnet --version
dotnet run --urls http://127.0.0.1:42101
```

`dotnet --version` should print 11.0.100 or a later 11.0 version. Use any free loopback port.

## Layout

| Chapters | Folders |
| --- | --- |
| 1-6 | `ChapterNN/FullExample` and `ChapterNN/Activity`. Chapter 2 keeps its separate Core, Infrastructure and Web projects. |
| 7-12 | `ChapterNN` holds each chapter's complete checkpoint. |
| 13-18 | `ChapterNN` holds the chapter's projects, plus any `Activity`, `Tests`, `Benchmarks` or `Checks` projects the chapter uses. |

Each folder focuses on one chapter. Follow the chapter's steps rather than merging every checkpoint into one project. The in-memory stores and test authentication handlers exist for local learning; they are not production persistence or authentication.

## About the book

The book explains every example and the decisions behind it. More programming books are at [unqbd.com](https://www.unqbd.com).

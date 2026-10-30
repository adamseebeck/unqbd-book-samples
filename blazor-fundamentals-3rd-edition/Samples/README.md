# Samples

Companion code for *Blazor Fundamentals, Third Edition*. Paths in the book such as `Samples/Checkpoints/Chapter03` start here.

## How the checkpoints fit together

Each folder in `Checkpoints` is the Chapter 2 project plus that chapter's files only; it doesn't include earlier chapters' pages. Each chapter's Program.cs listings are complete for that chapter, so pasting a later Program.cs over a project that has earlier chapters' pages removes services those pages need. To follow a chapter in your own project, start from a fresh copy of your Chapter 2 project, add the chapter's files and replace both Program.cs files with the chapter's versions. `LibraryDesk` combines every chapter; Chapter 23 prints its Program.cs files.

In a checkpoint the server project is `LibraryDesk/LibraryDesk.csproj`. In the project you create in Chapter 2 it is one folder deeper: `LibraryDesk/LibraryDesk/LibraryDesk.csproj`.

## Requirements

- The .NET 11 SDK, version 11.0.100 or later. Each `global.json` selects it.
- Python 3, for `ActivityInvestigations/create_copy.py` and for serving the published PWA in Chapters 20 and 23.

Build in a working copy so bin, obj and publish output stay out of the downloaded companion code.

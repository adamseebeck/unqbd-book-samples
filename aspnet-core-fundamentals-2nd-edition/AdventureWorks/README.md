# AdventureWorks: the running project

One AdventureWorks outdoor-gear store app that grows through the book. The reader creates it in Chapter 1, and every chapter starts where the previous one ended. Each `ChapterNN` folder is a full snapshot of the app at the end of that chapter: the previous chapter's snapshot plus that chapter's change. It builds and runs on its own.

This folder currently holds Chapters 1 to 6. The `Chapter01` to `Chapter18` folders beside it belong to the current printing of the book and are unchanged.

## Folders

| Folder | What it is |
| --- | --- |
| `ChapterNN` | The checkpoint at the end of Chapter NN. The next chapter starts here. |
| `ChapterNN-Activity` | A copy of that checkpoint plus a worked solution to the chapter's Activity. The next chapter does not start from it. |
| `notes/ChapterNN-delta.md` | For the book's writers: every file and line the chapter adds or changes, in order, with the commands, their output and the real responses. |

Each checkpoint folder is the reader's working folder: it holds `global.json` and, from Chapter 2, `AdventureWorks.slnx` with three projects:

| Project | Holds |
| --- | --- |
| `AdventureWorks` | Web: hosting, middleware, endpoints, pages and composition |
| `AdventureWorks.Core` | Domain types and contracts, with no ASP.NET Core dependency |
| `AdventureWorks.Infrastructure` | Implementations of the Core contracts (in memory until Chapter 10) |

## Run a checkpoint

You need the .NET 11 SDK, version 11.0.100 or later; `global.json` selects it. From a checkpoint folder:

```shell
dotnet run --project AdventureWorks
```

The app listens on https://localhost:7001 and http://localhost:5001 in Development. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once. From Chapter 2 the checkpoints read one harmless User Secret (`FeatureFlags:MaxCartItems` = 30); each README shows the command that sets it.

Every checkpoint has a `requests.http` file with each request and the status you should see, and a README with the same requests as curl.exe commands for Windows PowerShell 5.1.

## What each chapter adds

| Chapter | Adds |
| --- | --- |
| 1 | The web host: Kestrel limits, request timing, `/`, `/api/status`, `/health` |
| 2 | Core and Infrastructure projects, validated FeatureFlags options, three service lifetimes, `/api/orders/{id}`, User Secrets |
| 3 | Correlation IDs, Problem Details for domain errors, status-code pages, a `/ping` branch and an `/api` branch |
| 4 | The product catalog (`/api/catalog`), a custom route constraint, named endpoints and links, OpenAPI, feature files |
| 5 | The product API as controllers (`/api/v1/products`) and Minimal APIs (`/api/v2/products`) over one store, with validation and filters |
| 6 | Razor Pages over the same store: product list and edit pages with a version check, a View Component and an Admin area |

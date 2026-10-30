# Chapter 2 Executable Checkpoint

Copy this complete directory to a working folder, keep its global.json active, and run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631`. Use one host at a time or choose a different port. The generated App shell and client assembly are included; focused files are listed in the chapter. Data Protection keys in this local example are ephemeral; a real deployment needs durable key storage.

This is the clean Chapter 2 project. Every other checkpoint is this project plus one chapter's files, so later chapters start from a fresh copy of it. Samples/LibraryDesk combines all chapters.

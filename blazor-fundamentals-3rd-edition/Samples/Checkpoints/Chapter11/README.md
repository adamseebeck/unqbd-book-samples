# Chapter 11 Executable Checkpoint

Copy this complete directory to a working folder, keep its global.json active, and run `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631`. Use one host at a time or choose a different port. The generated App shell and client assembly are included; focused files are listed in the chapter. Data Protection keys in this local example are ephemeral; a real deployment needs durable key storage.

This checkpoint is the Chapter 2 project plus Chapter 11's files only; it doesn't include earlier chapters' pages. Samples/LibraryDesk combines all chapters.

Stop the Development host before you run it in Production: `dotnet run --project LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Production --urls http://127.0.0.1:58631`. This checkpoint has no Production feedback page. Check that a GET to each lab path returns 404 and that the local sign-in controls are gone. Chapter 23 tests a valid-token POST against the finished app's /static-feedback form.

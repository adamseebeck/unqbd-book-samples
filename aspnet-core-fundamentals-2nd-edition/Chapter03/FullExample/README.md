# Chapter 3: Full Example

The complete code for Chapter 3's Full Example, matching the listings in the book.

Run `dotnet run` from this folder. The launch profile in Properties/launchSettings.json is the one Chapter 1 builds: it starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once.

The `global.json` here selects the .NET 11 SDK (11.0.100 or later).

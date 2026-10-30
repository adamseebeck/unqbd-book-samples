# Chapter 6: Activity solution

A worked solution to Chapter 6's activity. Try the activity yourself first. It uses fictional local data.

Run `dotnet run` from this folder. The launch profile in Properties/launchSettings.json is the one Chapter 1 builds: it starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once.

The `global.json` here selects the .NET 11 SDK (11.0.100 or later).

Antiforgery token validation stays on, so a valid POST needs a real form cookie and token pair.

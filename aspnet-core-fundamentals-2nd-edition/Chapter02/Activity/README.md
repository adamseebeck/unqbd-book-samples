# Chapter 2: Activity solution

A worked solution to Chapter 2's activity. Try the activity yourself first. It uses fictional local data.

Run `dotnet run --project AdventureWorks/AdventureWorks.csproj` from this folder. The Core and Infrastructure projects are referenced by the web project. The launch profile in Properties/launchSettings.json is the one Chapter 1 builds: it starts the app in Development at https://localhost:7001 and http://localhost:5001. If the browser or curl.exe rejects the local certificate, run `dotnet dev-certs https --trust` once.

The `global.json` here selects the .NET 11 SDK (11.0.100 or later).

The activity uses harmless example settings in User Secrets.

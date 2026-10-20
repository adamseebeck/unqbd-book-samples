# Chapter 2: Activity solution

A worked solution to Chapter 2's activity. Try the activity yourself first. It uses fictional local data.

Run `dotnet run --project AdventureWorks/AdventureWorks.csproj --urls http://127.0.0.1:42101` from this folder. The Core and Infrastructure projects are referenced by the web project.

The `global.json` here selects the .NET 11 SDK (11.0.100 or later). Test transport security with a local HTTPS profile: HTTPS redirection needs a real HTTPS endpoint, so an HTTP-only run does not check TLS or HSTS.

The activity uses harmless example settings in User Secrets.

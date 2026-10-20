# Chapter 2: Full Example

The complete code for Chapter 2's Full Example, matching the listings in the book.

Run `dotnet run --project AdventureWorks/AdventureWorks.csproj --urls http://127.0.0.1:42101` from this folder. The Core and Infrastructure projects are referenced by the web project.

The `global.json` here selects the .NET 11 SDK (11.0.100 or later). Test transport security with a local HTTPS profile: HTTPS redirection needs a real HTTPS endpoint, so an HTTP-only run does not check TLS or HSTS.

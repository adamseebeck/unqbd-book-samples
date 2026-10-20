# Chapter 5: Full Example

The complete code for Chapter 5's Full Example, matching the listings in the book.

Run `dotnet run --urls http://127.0.0.1:42101` from this folder, or use any free loopback port.

The `global.json` here selects the .NET 11 SDK (11.0.100 or later). Test transport security with a local HTTPS profile: HTTPS redirection needs a real HTTPS endpoint, so an HTTP-only run does not check TLS or HSTS.

This project restores Microsoft.AspNetCore.OpenApi 11.0.0.

# Chapter 4: Activity solution

A worked solution to Chapter 4's activity. Try the activity yourself first. It uses fictional local data.

Run `dotnet run --urls http://127.0.0.1:42101` from this folder, or use any free loopback port.

The `global.json` here selects the .NET 11 SDK (11.0.100 or later). Test transport security with a local HTTPS profile: HTTPS redirection needs a real HTTPS endpoint, so an HTTP-only run does not check TLS or HSTS.

This project restores Microsoft.AspNetCore.OpenApi 11.0.0.

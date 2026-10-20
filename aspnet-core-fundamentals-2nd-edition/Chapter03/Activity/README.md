# Chapter 3: Activity solution

A worked solution to Chapter 3's activity. Try the activity yourself first. It uses fictional local data.

Run `dotnet run --urls http://127.0.0.1:42101` from this folder, or use any free loopback port.

The `global.json` here selects the .NET 11 SDK (11.0.100 or later). Test transport security with a local HTTPS profile: HTTPS redirection needs a real HTTPS endpoint, so an HTTP-only run does not check TLS or HSTS.

The `X-Exercise-Identity` header is a local test double, and echoing the idempotency key does not make the endpoint durably idempotent.

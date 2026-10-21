# Chapter 11 Activity Answer

- Run the anonymous, reader, and librarian cases against both /staff and /api/staff/count. Verify that hiding or showing AuthorizeView does not change the API decision.
- Try a sign-in POST without its antiforgery token and check that it's rejected. The supplied server Program.cs, Security, Staff, and Routes files are the answer reference.
- Use only the local test identities; don't connect a real account or production identity provider.
- Add a Readers policy requiring an authenticated user and a server-only /api/profile endpoint that returns the current name and requires that policy.
- Verify anonymous 401, reader/librarian profile names, and unchanged Staff 403/200 results using the local antiforgery-protected cookie sign-in.

## Solved Source and Execution

The solved code change applies to a complete copy of the project; use the copy script instead of editing the original sample.

The exact code change and its copy script are in `../ActivityInvestigations/Chapter11`. Its README describes the expected result and how to start over.

## Reference Implementation

- `../Checkpoints/Chapter11/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter11/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter11/LibraryDesk/Components/Pages/Security.razor`
- `../Checkpoints/Chapter11/LibraryDesk/Components/Pages/Staff.razor`
- `../Checkpoints/Chapter11/LibraryDesk/Components/Routes.razor`
- `../Checkpoints/Chapter11/LibraryDesk/Components/Pages/Denied.razor`

## Expected Reasoning

The server page and API enforce the Staff policy independently, while components display the current result. The local test sign-in makes those checks testable without implementing a real identity provider.

## Production Notes

The cookie sign-in example enforces policy for SSR and server requests only. The Auto-capable catalog is public, and the client Program.cs doesn't configure an authentication-state deserialization provider, so the server policy tests say nothing about a WebAssembly login flow. An application's client authentication state also remains advisory to the API, which must validate the request itself. The example uses ephemeral Data Protection keys so it never writes to the machine key store; its cookies become invalid after the process restarts.

Never turn the passwordless test sign-in into a login system.

Real deployments need HTTPS, secure cookie settings appropriate to the hosting arrangement, durable Data Protection keys, safe local return URLs, and revocation or revalidation behavior. A long-lived server circuit can outlive changes in the external identity system; design how the application learns that permission changed. The local tests don't cover an external identity provider or production session lifetimes.

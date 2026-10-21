# Chapter 11 Activity Answer

- Run the anonymous, reader, and librarian cases against both /staff and /api/staff/count. Verify that hiding or showing AuthorizeView does not change the API decision.
- Try a sign-in POST without its antiforgery token and require rejection. The supplied server Program.cs, Security, Staff, and Routes files are the answer reference.
- Do not change a real account or connect a production issuer.
- Add a Readers policy requiring an authenticated user and a server-only /api/profile endpoint that returns the current name and requires that policy.
- Verify anonymous 401, reader/librarian profile names, and unchanged Staff 403/200 results using the local antiforgery-protected cookie fixture.

## Solved Source and Execution

The exact solved code delta below applies to a complete copied target. Follow the command rather than changing canonical source.

The activity has an exact executable delta and copy runner at `../ActivityInvestigations/Chapter11`. Its README explains the expected observation and restoration.

## Reference Implementation

- `../Checkpoints/Chapter11/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter11/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter11/LibraryDesk/Components/Pages/Security.razor`
- `../Checkpoints/Chapter11/LibraryDesk/Components/Pages/Staff.razor`
- `../Checkpoints/Chapter11/LibraryDesk/Components/Routes.razor`
- `../Checkpoints/Chapter11/LibraryDesk/Components/Pages/Denied.razor`

## Expected Reasoning

The server page and API enforce the Staff policy independently, while components display the current result. The isolated fixture makes those checks testable without pretending to implement a real identity provider.

## Verification Boundary

The cookie fixture demonstrates policy enforcement for SSR/server requests only. The Auto-capable catalog is public and the client Program.cs does not configure an authentication-state deserialization provider. Do not infer a working WebAssembly login flow from the server policy tests. An application's client authentication state also remains advisory to the API, which must validate the request itself. The fixture uses ephemeral Data Protection keys to avoid machine key-store writes; its cookies become invalid after the process restarts.

Never promote the passwordless fixture into a login system.

Real deployments need HTTPS, secure cookie settings appropriate to the hosting arrangement, durable Data Protection keys, safe local return URLs, and revocation or revalidation behavior. A long-lived server circuit can outlive changes in the external identity system; design how the application learns that permission changed. The local tests do not certify an external issuer or production session lifecycle.

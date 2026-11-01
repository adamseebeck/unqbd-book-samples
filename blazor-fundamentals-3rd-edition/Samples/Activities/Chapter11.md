# Chapter 11 activity answer

Use a copy of Samples/ActivityStarts/Chapter11 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity adds a second policy, Readers, that any signed-in user meets, and a GET /api/profile endpoint that requires it. You write both in a copy of the Chapter 11 checkpoint.

## The change

In the server's LibraryDesk/LibraryDesk/Program.cs, the AddAuthorization call gets a block with both policies:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Staff",
        policy => policy.RequireRole("Librarian"));
    options.AddPolicy("Readers",
        policy => policy.RequireAuthenticatedUser());
});
```

The new endpoint goes just before the /api/staff/count endpoint:

```csharp
app.MapGet("/api/profile", (HttpContext context) =>
        Results.Ok(new { Name = context.User.Identity!.Name }))
    .RequireAuthorization("Readers");
```

## What you should see

- http://127.0.0.1:58631/api/profile before signing in: the Network tab shows status 401.
- Signed in on /security as the reader: /api/profile shows {"name":"reader"}.
- Signed in as the librarian: /api/profile shows {"name":"librarian"}.
- /api/staff/count still gives the reader 403 and the librarian 200.

## Files involved

- Samples/ActivityInvestigations/Chapter11: the copy's README. The copy is Samples/Checkpoints/Chapter11 unchanged, and you make the change in its LibraryDesk/LibraryDesk/Program.cs.
- Samples/ActivitySolutions/Chapter11: the same project with exactly this change, to compare with yours.
- Samples/Checkpoints/Chapter11/LibraryDesk/LibraryDesk/Program.cs: the file before the change.

## Why

A policy is a named rule. RequireAuthenticatedUser passes for anyone the cookie handler has signed in, whatever their role; RequireRole("Librarian") passes only for the librarian. Without a sign-in cookie, the request has no identity, so the endpoint answers 401 (not signed in). The reader is signed in but isn't a librarian, so /api/staff/count answers 403 (signed in, not allowed). The endpoint reads the name from context.User, which the server built from the cookie; it never trusts a name sent by the browser. JSON serialization writes the Name property as name.

# Chapter 11 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. You add a second policy, Readers, and an endpoint that any signed-in user may call.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter11 to a new working folder such as C:\BookExercises\Chapter11. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. It contains the unchanged chapter checkpoint.
- In the copy's LibraryDesk/Program.cs, add the Readers policy, built with `policy.RequireAuthenticatedUser()`, beside the Staff policy. AddPolicy returns nothing, so you can't chain a second call: turn the AddAuthorization lambda into a block, `options => { ...; ...; }`, and put both AddPolicy calls inside it.
- In the same file, add GET /api/profile just before the /api/staff/count endpoint. It takes an HttpContext parameter, returns `Results.Ok(new { Name = context.User.Identity!.Name })` and requires the Readers policy.
- Save and press F5. If the build fails, read View > Error List and the Build output. Double-click an error to open its file and line, fix it and build again. A missing semicolon, for example, produces CS1002. The browser opens only after the build succeeds.
- Open http://127.0.0.1:58631/api/profile before signing in: the Network tab shows status 401. Sign in on /security as the reader and open /api/profile again: the browser shows {"name":"reader"}. As the librarian it shows {"name":"librarian"}. /api/staff/count still gives the reader 403 and the librarian 200.
- Compare with Samples/Activities/Chapter11.md. Samples/ActivitySolutions/Chapter11 is the copy with the change made.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

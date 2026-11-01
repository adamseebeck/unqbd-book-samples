# Chapter 6 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. You add a Remove C# button and check that its change reaches the summary through the service.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter06 to a new working folder such as C:\BookExercises\Chapter06. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. It contains the chapter checkpoint and a separate exercise Tests project.
- In the copy's LibraryDesk.Client/Pages/FavoritesDemo.razor, add a Remove C# button above `<FavoritesSummary />` that calls Favorites.Remove(1), written the same way as the Favorite buttons. Run the copy with F5 using the http profile and open http://127.0.0.1:58631/favorites: the page shows Favorite C#, Favorite Blazor and Remove C# above the summary.
- Click Favorite C# and Favorite Blazor: the summary shows Favorites: 2 and Saved IDs: 1, 2. Click Remove C# twice: the summary shows Favorites: 1 and Saved IDs: 2, and the second click changes nothing.
- Open /favorites in a second tab: it starts at Favorites: 0. Press Shift+F5 in Visual Studio when you're done.
- The copy's Tests folder has a component test: a small program that renders the page twice in two separate scopes, clicks your Remove C# button in one, and checks both summaries (Chapter 18 explains component tests). In Test Explorer, right-click Chapter06.Tests and choose Run. Expect one passed exercise test. If you run it before you add the button, the exercise test fails. Select the failed test to read its message and stack trace; a compiler error appears in Error List instead.
- Compare your button with Samples/Activities/Chapter06.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

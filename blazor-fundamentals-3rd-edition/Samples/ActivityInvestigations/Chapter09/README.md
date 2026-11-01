# Chapter 9 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter09 to a new working folder such as C:\BookExercises\Chapter09. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. The server's Services/MemoryCatalogQuery.cs also contains an unused ShortCatalogQuery implementation that returns Routing Field Notes and State Field Notes.
- In the copy's server Program.cs, change MemoryCatalogQuery to ShortCatalogQuery in the AddSingleton registration. Run the copy with F5 using the http profile and open /services: it shows Registered query returns 2 books. and the two new titles, although ServicesDemo.razor didn't change. Open /books/3 too: it now shows No book has this identifier., because BookDetails uses the same registration.
- Stop the copy. In the same Program.cs, change `builder.Services.AddScoped<FavoritesState>();` to `builder.Services.AddSingleton<FavoritesState>();` and run the copy again. Open /favorites in two tabs and click Favorite C# in the first. The second tab changes to Favorites: 1 as well, without a reload. Every visitor to this server would now share one favorites list, which is why FavoritesState is scoped.
- Compare with Samples/Activities/Chapter09.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

## Optional generator

Instead of copying the prepared start, open PowerShell in Samples/ActivityInvestigations and run:

```powershell
python create_copy.py --chapter 9 --output C:/BookExercises/Chapter09
```

Choose an output path that does not exist. The script copies the checkpoint, verifies and applies its checked activity changes, adds the exercise tests and includes them in LibraryDesk.slnx. It never edits the download. Open that generated solution and follow the same task. Python is optional for preparing exercises because ActivityStarts are already supplied.

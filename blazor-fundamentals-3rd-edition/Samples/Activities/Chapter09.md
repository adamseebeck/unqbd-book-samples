# Chapter 9 activity answer

Use a copy of Samples/ActivityStarts/Chapter09 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity swaps the registered ICatalogQuery for a second implementation, then registers FavoritesState as a singleton, both in a copy, to see what a registration controls.

## What you should see

After you change MemoryCatalogQuery to ShortCatalogQuery in the AddSingleton registration in the copy's server Program.cs:

- /services shows Registered query returns 2 books. and the titles Routing Field Notes and State Field Notes. ServicesDemo.razor didn't change.
- /books/3 shows No book has this identifier., because BookDetails uses the same registration. (/books/1 now shows Routing Field Notes.)

After you also change `builder.Services.AddScoped<FavoritesState>();` to `builder.Services.AddSingleton<FavoritesState>();`:

- Open /favorites in two tabs and click Favorite C# in the first. The second tab changes to Favorites: 1 as well, without a reload.

## Files involved

- Samples/ActivityInvestigations/Chapter09: the copy's change and its README.
- In the copy: LibraryDesk/LibraryDesk/Services/MemoryCatalogQuery.cs, which holds ShortCatalogQuery above MemoryCatalogQuery, and LibraryDesk/LibraryDesk/Program.cs, where you change the two registrations.
- Unchanged consumers: LibraryDesk/LibraryDesk.Client/Pages/ServicesDemo.razor, LibraryDesk/LibraryDesk/Components/Pages/BookDetails.razor and LibraryDesk/LibraryDesk.Client/Components/FavoritesSummary.razor.

## Why

Components ask for ICatalogQuery, not for a class. The registration in Program.cs decides which class they get, so changing one line changed every page that injects the service. That is the point of registering an interface.

The lifetime matters as much as the class. A scoped FavoritesState gives each circuit, and so each tab, its own list. A singleton is one instance for the whole server, so both tabs share it, and FavoritesSummary in the second tab hears the Changed event raised by the first. On a real site every visitor would now share one favorites list, which is why FavoritesState is scoped.

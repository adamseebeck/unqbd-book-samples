# Chapter 9 activity: a second ICatalogQuery and a singleton FavoritesState

Do this activity in a copy, so your LibraryDesk project stays as it is. The copy is Samples/Checkpoints/Chapter09, the project at the end of Chapter 9, with the change below. Stop your own app with Ctrl+C first: the copy uses the same address.

## Make the copy

In a terminal, go to the companion code's Samples/ActivityInvestigations folder and run:

```powershell
python create_copy.py --chapter 9 --output C:/BookExercises/Chapter09
cd C:/BookExercises/Chapter09
```

Choose an output folder that doesn't exist yet, outside the companion code. The script checks that the files it changes are the ones this activity was written for, copies the checkpoint, applies the change and prints the files it changed. The copy also gets this README and an investigation-manifest.json file that lists any change the script made.

## What the copy changes

LibraryDesk/LibraryDesk/Services/MemoryCatalogQuery.cs holds a second class above MemoryCatalogQuery: ShortCatalogQuery, another implementation of ICatalogQuery that returns two books, Routing Field Notes and State Field Notes. Nothing uses it until you change the registration in LibraryDesk/LibraryDesk/Program.cs.

## Run it

Run the copy with the command from Chapter 2, from the copy's folder, typed as one line:

```powershell
dotnet run --project LibraryDesk/LibraryDesk/LibraryDesk.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58631
```

Press Ctrl+C in the terminal to stop it. Stop it, too, before you change a C# or Razor file, then run it again.

## What you should see

After you change MemoryCatalogQuery to ShortCatalogQuery in the AddSingleton registration, http://127.0.0.1:58631/services shows Registered query returns 2 books. and the titles Routing Field Notes and State Field Notes, although ServicesDemo.razor didn't change. /books/3 shows No book has this identifier., because BookDetails uses the same registration. After you change builder.Services.AddScoped<FavoritesState>(); to builder.Services.AddSingleton<FavoritesState>();, open /favorites in two tabs and click Favorite C# in the first: the second tab changes to Favorites: 1 as well, without a reload. Every visitor would now share one favorites list.

## Start over

To undo your edits, make a fresh copy: run create_copy.py again with a new --output folder, or delete the copy's folder and reuse its name. The companion code and your own project are never changed. Samples/Activities/Chapter09.md explains the answer.

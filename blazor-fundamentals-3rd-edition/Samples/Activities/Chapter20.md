# Chapter 20 activity answer

The activity lowers the reading list's limit from 50 titles to 3, in a copy, and checks that the limit holds and the list survives a reload.

## Commands

From the copy's folder, run the offline app with the dotnet run command from Chapter 20, typed as one line:

```powershell
dotnet run --project LibraryDesk.Pwa/LibraryDesk.Pwa.csproj --no-launch-profile -- --environment Development --urls http://127.0.0.1:58634
```

Run the copy's test, with the app stopped:

```powershell
dotnet test Tests/Chapter20.Tests.csproj
```

## What you should see

- Open http://127.0.0.1:58634. Save titles until the list has three, then try a fourth: the status reads This local list is limited to 3 titles. and the list still has three.
- Reload: the same three titles load.
- The test saves three titles, tries a fourth, and checks that only three writes reached localStorage. Its last line starts with Passed! and reports Passed: 1.

## Files involved

- Samples/ActivityInvestigations/Chapter20: the copy's change, its README and its Tests folder.
- In the copy: LibraryDesk.Pwa/Pages/Home.razor, where both the check (titles.Length >= 3) and the message (limited to 3 titles.) change.

## Why

Save checks the limit before it builds the new list or calls localStorage, so the fourth title never reaches the browser's storage and the list on screen stays at three. The list in localStorage belongs to the origin, http://127.0.0.1:58634, so a reload reads the same three titles back. Change the number in both places, the check and the message, or the page reports a limit it doesn't enforce.

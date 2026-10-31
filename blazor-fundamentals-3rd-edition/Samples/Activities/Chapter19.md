# Chapter 19 activity answer

The task is to keep only the latest five notices instead of twenty, and to see the hub reject a notice that is too long. Here is the change; compare it with yours.

## The change

In the copy's LibraryDesk/LibraryDesk.Client/Pages/LiveCatalog.razor, the line that drops the oldest notice reads:

```csharp
if (notices.Count == 5) notices.RemoveAt(0);
```

For the second part, the input loses its maxlength:

```razor
<input id="notice" @bind="text" />
```

## What you should see

- Open http://127.0.0.1:58631/live in two tabs. From the first tab, broadcast Notice 1, Notice 2 and so on up to Notice 6. Both lists show Notice 2 to Notice 6; Notice 1 has been dropped.
- Stop the copy, delete maxlength="120" from the input, and run it again. Reload the /live tab so the page has the new input. In the Console, copy("x".repeat(121)) puts a 121-character text on the clipboard. Paste it into the Notice box with Ctrl+V and click Broadcast locally: the status says Use a message from 1 to 120 characters.

## Files involved

- Samples/ActivityInvestigations/Chapter19: the README and delta.json that create_copy.py uses. The copy itself is Samples/Checkpoints/Chapter19, unchanged.
- In the copy: LibraryDesk/LibraryDesk.Client/Pages/LiveCatalog.razor (the limit, and the maxlength you delete).
- Unchanged: LibraryDesk/LibraryDesk/Hubs/CatalogHub.cs, whose Announce method checks the length.

## Why

Each page keeps its own list of notices. Before it adds a new one, it removes the oldest when the list is full, so the list never grows past the limit. Both tabs receive the same six notices from the hub, so both drop Notice 1.

maxlength only stops typing in the browser, and anyone can remove it, as you did. A tab left open keeps the old input until you reload it, so the browser would still cut the paste to 120 characters. CatalogHub checks the length again on the server and throws a HubException. The page's Send method catches it and shows Use a message from 1 to 120 characters. as the status. Keep every rule that protects the server on the server.

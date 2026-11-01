# Chapter 20 activity answer

Use a copy of Samples/ActivityStarts/Chapter20 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The activity lowers the reading list's limit from 50 titles to 3, in a copy, and checks that the limit holds and the list survives a reload.

## The change

In the copy's LibraryDesk.Pwa/Pages/Home.razor, both the check and the message change:

```razor
        if (titles.Length >= 3)
        {
            status = "This local list is limited to 3 titles.";
            return;
        }
```

## Run and check

Select LibraryDesk.Pwa as the startup project, choose its http profile and press F5. The development address is http://127.0.0.1:58634. Stop debugging and build before running Chapter20.Tests in Test Explorer. This one exercise test is separate from the two LibraryDesk.Pwa.Tests checks.

## What you should see

- Open http://127.0.0.1:58634. Save titles until the list has three, then try a fourth: the status reads This local list is limited to 3 titles. and the list still has three.
- Reload: the same three titles load.
- The test saves three titles, tries a fourth, and checks that only three writes reached localStorage and that a new page loads the three. Test Explorer reports one passed exercise test.

## Files involved

- Samples/ActivityInvestigations/Chapter20: the README, delta.json and Tests folder that create_copy.py uses. The copy itself is Samples/Checkpoints/Chapter20, unchanged.
- In the copy: LibraryDesk.Pwa/Pages/Home.razor, where both the check (titles.Length >= 3) and the message (limited to 3 titles.) change.

## Why

Save checks the limit before it builds the new list or calls localStorage, so the fourth title never reaches the browser's storage and the list on screen stays at three. The list in localStorage belongs to the origin, http://127.0.0.1:58634, so a reload reads the same three titles back. Change the number in both places, the check and the message, or the page reports a limit it doesn't enforce.

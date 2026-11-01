# Chapter 16 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. The copy's Modes.razor already passes a Step value to each island; you give ModeCounter the parameter that receives it.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter16 to a new working folder such as C:\BookExercises\Chapter16. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. Modes.razor passes Step="2", Step="3" and Step="4" to the Server, WebAssembly and Auto islands. The supplied test passes Step by name, so the unsolved project can build while the missing parameter remains a runtime error. Run it with F5.
- Open http://127.0.0.1:58631/modes. Instead of the page, you see An unhandled exception occurred while processing the request. and InvalidOperationException: Object of type 'LibraryDesk.Client.Components.ModeCounter' does not have a property matching the name 'Step'. The server can't prerender the page, because ModeCounter has no Step parameter yet.
- Stop the copy with Shift+F5. In the copy's LibraryDesk.Client/Components/ModeCounter.razor, add `[Parameter] public int Step { get; set; } = 1;` inside the @code block, and change the button's `count++` to `count += Step`. A plain int is data, so it can cross into every island.
- Run the copy again, open /modes, wait until no counter reads Static, and click each button once. The lines read Renderer: Server. Count: 2, Renderer: WebAssembly. Count: 3 and Renderer: WebAssembly. Count: 4. Press F5: all three counts return to 0, because the reload created new instances.
- Stop debugging, build the solution and open Test Explorer. Run the Chapter16.Tests project: its three cases check one click with Step 2, 3 and 4. Expect three passed tests after the fix. Before it, they fail on the same missing-parameter error. Chapter 18 explains these component tests.
- Compare with Samples/Activities/Chapter16.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

## Optional generator

Instead of copying the prepared start, open PowerShell in Samples/ActivityInvestigations and run:

```powershell
python create_copy.py --chapter 16 --output C:/BookExercises/Chapter16
```

Choose an output path that does not exist. The script copies the checkpoint, verifies and applies its checked activity changes, adds the exercise tests and includes them in LibraryDesk.slnx. It never edits the download. Open that generated solution and follow the same task. Python is optional for preparing exercises because ActivityStarts are already supplied.

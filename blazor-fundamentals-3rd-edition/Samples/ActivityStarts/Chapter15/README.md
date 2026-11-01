# Chapter 15 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. In the copy, VirtualDemo renders every row with an ordinary @foreach loop; you count its elements, then bring Virtualize back with more overscan.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter15 to a new working folder such as C:\BookExercises\Chapter15. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. VirtualDemo.razor in this start uses a foreach loop instead of Virtualize. Run it with F5.
- Open http://127.0.0.1:58631/virtual, press F12, open Console and enter `document.getElementById("results").childElementCount`. It prints 10000: every row is in the DOM, and there are no spacers. The unchanged chapter project renders 18 elements, 16 rows and two spacers. Write down both numbers.
- Stop the copy with Shift+F5. In the copy's LibraryDesk.Client/Pages/VirtualDemo.razor, replace the @foreach line and its braces with `<Virtualize Items="books" Context="book" ItemSize="40" OverscanCount="10">` and `</Virtualize>`, keeping the existing row div between those tags. Predict the count before you run it.
- Run the copy again, open /virtual and run the Console line. It prints 32: 30 rows and the 2 spacers. Drag the scroll bar to the middle and run it again: still 32. This comparison counts elements; it isn't a timing benchmark, which needs controlled measurements on a realistic workload.
- Compare with Samples/Activities/Chapter15.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

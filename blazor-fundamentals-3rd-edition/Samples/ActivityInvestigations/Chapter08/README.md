# Chapter 8 activity: unsolved start

This is a prepared exercise start, not a solved checkpoint. Follow the task below in a copy. Use the released .NET 11 SDK baseline from the book.

Do this in a copy, so your LibraryDesk project stays as it is. You raise the message minimum from 5 to 10 characters in the shared model and check both forms.

- Stop your main IDE run with Shift+F5. In File Explorer, copy Samples/ActivityStarts/Chapter08 to a new working folder such as C:\BookExercises\Chapter08. Open the copy's LibraryDesk/LibraryDesk.slnx in Visual Studio and select LibraryDesk with its http profile. It contains the unchanged chapter checkpoint and an exercise Tests project.
- In the copy's LibraryDesk.Client/Models/FeedbackInput.cs, change `MinimumLength = 5` to `MinimumLength = 10` on the Message property and save the file. Then press F5. The debugger browser opens at the same loopback address.
- Open http://127.0.0.1:58631/feedback and enter Ada and ada@example.com. With the message 123456789 (nine characters), Save shows The field Message must be a string with a minimum length of 10 and a maximum length of 300. and Nothing saved. With 1234567890 (ten characters), the status reads Feedback accepted locally for Ada. No account was created.
- Open http://127.0.0.1:58631/static-feedback, press F12, click inside DevTools, press Ctrl+Shift+P, search for javascript, choose Disable JavaScript and reload the page. Repeat the nine- and ten-character submissions. The setting applies while DevTools stays open; close DevTools and reload to restore JavaScript afterward. Nine characters show the same message and No submission.; ten characters show Static form accepted locally. One change to the shared model changed both forms.
- Compare with Samples/Activities/Chapter08.md.

## Start again

Copy the original Samples/ActivityStarts folder for this chapter to another new working folder. Keep your main cumulative project separate.

## Optional generator

Instead of copying the prepared start, open PowerShell in Samples/ActivityInvestigations and run:

```powershell
python create_copy.py --chapter 8 --output C:/BookExercises/Chapter08
```

Choose an output path that does not exist. The script copies the checkpoint, verifies and applies its checked activity changes, adds the exercise tests and includes them in LibraryDesk.slnx. It never edits the download. Open that generated solution and follow the same task. Python is optional for preparing exercises because ActivityStarts are already supplied.

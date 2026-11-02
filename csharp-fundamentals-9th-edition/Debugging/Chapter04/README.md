# AverageDebug

Remove (double) from the average assignment to reproduce the book's wrong result, 2. Restore the cast and confirm 2.5. Break on the assignment, inspect total/count in Watch, then step and inspect average.

In Visual Studio open AverageDebug.csproj, choose Debug configuration, and use F9, F5, F10, F11, Shift+F11, and Shift+F5 as the chapter explains. In VS Code open this folder with C# Dev Kit, use Run and Debug, and inspect VARIABLES, WATCH, and CALL STACK. Stop before editing, save, and start a fresh run. These experiments are separate from StudentTracker.

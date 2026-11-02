# LoopDebug

Change i <= 3 to i < 3 to reproduce 3 instead of 6. Break on total += i; inspect the counter before each addition. Restore <= and verify all three iterations.

In Visual Studio open LoopDebug.csproj, choose Debug configuration, and use F9, F5, F10, F11, Shift+F11, and Shift+F5 as the chapter explains. In VS Code open this folder with C# Dev Kit, use Run and Debug, and inspect VARIABLES, WATCH, and CALL STACK. Stop before editing, save, and start a fresh run. These experiments are separate from StudentTracker.

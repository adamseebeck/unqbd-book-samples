# ExceptionDebug

The caught divide-by-zero error is deliberate. Break on x / y and inspect both inputs. To inspect the throwing point, enable Thrown for System.DivideByZeroException in Exception Settings; continue into catch, then restore that setting. Removing try/catch reproduces the unhandled case from the chapter.

In Visual Studio open ExceptionDebug.csproj, choose Debug configuration, and use F9, F5, F10, F11, Shift+F11, and Shift+F5 as the chapter explains. In VS Code open this folder with C# Dev Kit, use Run and Debug, and inspect VARIABLES, WATCH, and CALL STACK. Stop before editing, save, and start a fresh run. These experiments are separate from StudentTracker.

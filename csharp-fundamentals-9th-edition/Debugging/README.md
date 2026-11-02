# Debugging practice

Start with the Chapter 3 greeting project and the book's two-input breakpoint walkthrough. These separate projects support the later arithmetic, loop, method, object, and exception investigations. Chapter 19 uses FullExamples/Chapter19/Chapter19.slnx: test the actual library, deliberately break Add, trace the inputs, repair it, and rerun the tests. Try the investigation before reading a completed example.

For interactive Console.ReadLine input in VS Code, set csharp.debug.console to integratedTerminal. A plain dotnet run does not attach a debugger. Use Debug configuration in either editor; remove exercise breakpoints and restore exception settings afterward.

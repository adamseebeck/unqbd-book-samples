# Chapter 4 Activity Answer

- Add a third ToggleMessage with a different book title and HiddenText. Toggle the middle instance twice and verify all three final states.
- The supplied component and composition are the reference solution; a correct extension creates another instance without static fields or a new service.

## Solved Source and Execution

Complete executable extension: `../ActivitySolutions/Chapter04`. Run and test commands are in that checkpoint's README.

## Reference Implementation

- `../Checkpoints/Chapter04/LibraryDesk/Program.cs`
- `../Checkpoints/Chapter04/LibraryDesk.Client/Program.cs`
- `../Checkpoints/Chapter04/LibraryDesk.Client/Components/ToggleMessage.razor`
- `../Checkpoints/Chapter04/LibraryDesk.Client/Pages/ComponentsDemo.razor`

## Expected Reasoning

Parameters enter from a parent, private state belongs to an instance, and lifecycle work belongs at the stage that provides its prerequisites. Independent toggle behavior checks that those boundaries are working.

## Verification Boundary

Component boundaries should follow meaningful UI responsibilities. Splitting every line of markup into a component adds overhead and makes the data flow harder to follow. Start with a cohesive unit such as ToggleMessage, then extract shared behavior when it has a clear contract. A small parameter surface is easier to test and maintain than a component that reaches into unrelated services.

Define the child before composing it.

Rendering does not make a component a security boundary. Parameter values reach markup unless you deliberately keep them server-only. Use text rendering for book descriptions; do not turn untrusted descriptions into MarkupString. The toggle changes presentation only and makes no claim about restricting access to the underlying data.

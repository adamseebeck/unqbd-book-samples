# Chapter 4 activity answer

The activity adds a third ToggleMessage to ComponentsDemo, with your own Message and a HiddenText of Tap to read, in a copy of Samples/Checkpoints/Chapter04.

## The change

In the copy's LibraryDesk/LibraryDesk.Client/Pages/ComponentsDemo.razor, add a third line under the two existing notes. The solved copy uses this message; yours can be any text:

```razor
<ToggleMessage Message="Database Field Notes is available."
               HiddenText="Tap to read" />
```

No other file changes. ToggleMessage already has the HiddenText parameter, with Details hidden as its default.

## What you should see

- /components shows three notes. The first two read Details hidden; the third reads Tap to read.
- Click the middle note's button twice and the third note's button once. The first note shows Details hidden, the middle one Details hidden (two clicks showed and hid it again), and the third shows your message, here Database Field Notes is available.
- The terminal shows six Initialized lines for one page visit: three from prerendering (C#, Blazor, then your note) and three more when the live page starts, in the same pattern step 6 showed for two notes.

## Files involved

- Samples/ActivitySolutions/Chapter04: the solved copy. Its changed file is LibraryDesk/LibraryDesk.Client/Pages/ComponentsDemo.razor. From that folder, dotnet test Tests/Chapter04.Tests.csproj runs its test; the last line reports Passed: 1.
- Samples/Checkpoints/Chapter04/LibraryDesk/LibraryDesk.Client/Components/ToggleMessage.razor: the component, unchanged.

## Why

Each ToggleMessage is a separate instance with its own private visible field, so a click on one note never changes another. Parameters set from the parent, Message and HiddenText, configure each instance; the visible field is state the instance keeps for itself. The terminal lines show the lifecycle running twice per note, once for prerendering and once when the circuit starts, which is why a one-time action never belongs in OnInitialized.

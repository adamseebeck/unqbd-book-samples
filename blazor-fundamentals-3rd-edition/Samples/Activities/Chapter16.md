# Chapter 16 activity answer

The activity gives ModeCounter a Step parameter, in a copy, and passes a different step to each island on /modes.

## The change

In the copy's LibraryDesk/LibraryDesk.Client/Components/ModeCounter.razor:

```razor
<button @onclick="() => count += Step">Increment mode counter</button>
@code {
    [Parameter] public int Step { get; set; } = 1;
    private int count;
}
```

In the copy's LibraryDesk/LibraryDesk/Components/Pages/Modes.razor, the three islands set Step="2" (Server), Step="3" (WebAssembly) and Step="4" (Auto).

## What you should see

- Open /modes and wait until no counter reads Static. Click each button once. The lines read Renderer: Server. Count: 2, Renderer: WebAssembly. Count: 3 and Renderer: WebAssembly. Count: 4.
- Press F5. All three counts return to 0.
- Chapter 18 shows how to check a parameter like Step with a component test. The copy already has one: from the copy's folder, dotnet test Tests/Chapter16.Tests.csproj checks one click with Step 2, 3 and 4, and the last line reports Passed: 3.

## Files involved

- Samples/ActivityInvestigations/Chapter16: the copy's changes, its README and its Tests folder.
- In the copy: the two files above.

## Why

A parameter that crosses into an island must be serializable data, because the island runs in another runtime: on the server over a circuit, or in the browser. A plain int is data, so it can cross into every island. Each island is a separate instance with its own count, so the steps never mix. Auto shows WebAssembly here by rule 1: the page also has a WebAssembly island. The reload creates new instances, so every count starts again at 0.

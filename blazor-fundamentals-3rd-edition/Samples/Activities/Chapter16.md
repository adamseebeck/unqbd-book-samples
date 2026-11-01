# Chapter 16 activity answer

Use a copy of Samples/ActivityStarts/Chapter16 and open its LibraryDesk/LibraryDesk.slnx in Visual Studio. F5 starts the selected app; Shift+F5 stops that debug run. The chapter gives the task, and this file explains its answer.

The copy's Modes.razor passes a Step value to each island on /modes. You give ModeCounter the parameter that receives it.

## What you should see

- As the copy is created, /modes shows An unhandled exception occurred while processing the request. and InvalidOperationException: Object of type 'LibraryDesk.Client.Components.ModeCounter' does not have a property matching the name 'Step'. The server can't prerender the page, because ModeCounter has no Step parameter.
- After your change, open /modes and wait until no counter reads Static. Click each button once. The lines read Renderer: Server. Count: 2, Renderer: WebAssembly. Count: 3 and Renderer: WebAssembly. Count: 4.
- Press F5. All three counts return to 0.
- Stop debugging, build and run Chapter16.Tests in Test Explorer. Its three cases check one click with Step 2, 3 and 4. All three pass after the fix. The test passes Step by name so the unsolved project can compile; before the fix it fails at runtime on the missing parameter, just like the page.

## The solved ModeCounter

```razor
<p data-renderer="@RendererInfo.Name">
    Renderer: @RendererInfo.Name. Count: @count
</p>
<button @onclick="() => count += Step">Increment mode counter</button>
@code {
    [Parameter] public int Step { get; set; } = 1;
    private int count;
}
```

The copy's LibraryDesk/LibraryDesk/Components/Pages/Modes.razor sets Step="2" (Server), Step="3" (WebAssembly) and Step="4" (Auto).

## Files involved

- Samples/ActivityInvestigations/Chapter16: the copy's change to Modes.razor, its README and its Tests folder.
- In the copy: LibraryDesk/LibraryDesk.Client/Components/ModeCounter.razor (your change) and LibraryDesk/LibraryDesk/Components/Pages/Modes.razor.

## Why

A component accepts only the parameters it declares with [Parameter], so a Step attribute on a component without that property fails when the server renders the page. A parameter that crosses into an island must be serializable data, because the island runs in another runtime: on the server over a circuit, or in the browser. A plain int is data, so it can cross into every island. Each island is a separate instance with its own count, so the steps never mix. Auto shows WebAssembly here because the page also has a WebAssembly island (rule 1). The reload creates new instances, so every count starts again at 0.

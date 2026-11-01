# Chapter 1 activity answer

The activity has two parts, written in a text file because there is no project yet: your catalog description as a paragraph element, and a render mode choice, from the rows of Figure 1.1, for three features.

## Your catalog description

Any two sentences that describe a catalog you would like to build are right. Write them in the same shape as the first paragraph of LibraryDesk's Home page: an opening `<p>`, your sentences on their own lines, then `</p>`. For example:

```razor
<p>
    A small catalog of the field guides in our team library,
    with the price and a short note for each.
    Anyone can browse it; only staff can change it.
</p>
```

Keep the file. In Chapter 3 you paste this element into LibraryDesk/Components/Pages/Home.razor in place of the first `<p>` element. The rest of the page stays as Chapter 1 printed it.

## Which row and which line each feature needs

| Feature | Row in Figure 1.1 | Render mode line | Why |
|---|---|---|---|
| A link to the catalog page | Static SSR | No line | Following a link is something plain HTML does. The browser requests the new address and the server renders it, so no C# needs to run after the page arrives. |
| A Favorite button that updates a count on the page | An interactive row: Interactive Server, Interactive WebAssembly or Interactive Auto | For example `@rendermode InteractiveServer` | A click must run C# to add the book and change the count. With static SSR the button would appear, but clicking it would do nothing, because nothing stays running after the HTML is sent. |
| A search box that filters the book list as you type | An interactive row, as for the Favorite button | For example `@rendermode InteractiveServer` | Every keystroke must run C# to filter the list and render the new result. Static SSR could only filter after a full form post. |

Any of the three interactive modes works for the button and the search box: `@rendermode InteractiveServer`, `@rendermode InteractiveWebAssembly` or `@rendermode InteractiveAuto`. The choice between them is about where the code runs: on the server over a circuit, in the browser after a download, or first one and then the other. LibraryDesk uses Interactive Server for its first interactive page in Chapter 3, and Chapter 16 compares all four modes side by side.

Put the line on the smallest component that needs it. A page with one Favorite button can stay static, with the line on the button's own component, as Chapter 16 shows.

## Files involved

- Samples/LibraryDesk/LibraryDesk/Components/Pages/Home.razor: the Home page from Chapter 1, the page your paragraph goes into.
- Samples/global.json: the SDK pin from Chapter 1.

## Why

The render mode decides where a component's C# runs. Make a component interactive only when a click or a keystroke must run C#; everything else can stay static, which costs no server memory and downloads nothing extra.

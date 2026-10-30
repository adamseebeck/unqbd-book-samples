# Chapter 1 activity answer

The activity has two parts: a two-sentence description of your own public catalog, and a render mode choice, from the rows of Figure 1.1, for three features.

## Your catalog description

Any two sentences that describe a catalog you would like to build are right. For example:

> A small catalog of the field guides in our team library, with the price and a short note for each. Anyone can browse it; only staff can change it.

Keep the text. In Chapter 3 you replace the first paragraph of LibraryDesk's Home page with it, inside the same `<p>` element. The rest of the page stays as Chapter 1 printed it.

## Which row each feature needs

| Feature | Row in Figure 1.1 | Why |
|---|---|---|
| A link to the catalog page | Static SSR | Following a link is something plain HTML does. The browser requests the new address and the server renders it, so no C# needs to run after the page arrives. |
| A Favorite button that updates a count on the page | An interactive row: Interactive Server, Interactive WebAssembly or Interactive Auto | A click must run C# to add the book and change the count. With static SSR the button would appear, but clicking it would do nothing, because nothing stays running after the HTML is sent. |
| A search box that filters the book list as you type | An interactive row, as for the Favorite button | Every keystroke must run C# to filter the list and render the new result. Static SSR could only filter after a full form post. |

Any of the three interactive rows works for the button and the search box. The choice between them is about where the code runs: on the server over a circuit (Interactive Server), in the browser after a download (Interactive WebAssembly), or first one and then the other (Interactive Auto). LibraryDesk uses Interactive Server for its first interactive page in Chapter 3, and Chapter 16 compares all four modes side by side.

## Files involved

- Samples/LibraryDesk/LibraryDesk/Components/Pages/Home.razor: the Home page from Chapter 1, the page your description goes into.
- Samples/global.json: the SDK pin from Chapter 1.

## Why

The render mode decides where a component's C# runs. Make a component interactive only when a click or a keystroke must run C#; everything else can stay static, which costs no server memory and downloads nothing extra.

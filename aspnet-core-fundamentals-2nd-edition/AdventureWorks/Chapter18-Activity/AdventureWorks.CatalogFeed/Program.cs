using AdventureWorks.CatalogFeed;

// Chapter 18 Activity: a read-only catalog feed small enough for
// Native AOT. The slim builder, a source-generated JSON context
// and no reflection; the store app itself is not AOT-compatible.
var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0,
        FeedJsonContext.Default));
var app = builder.Build();

FeedItem[] feed =
[
    new("HLM-1001", 29.99m), new("JRS-2001", 54.50m),
    new("BIK-3001", 1299.00m), new("TNT-4001", 189.00m)
];
app.MapGet("/feed", () => feed);
app.MapGet("/health/live", () => "Healthy");
app.Run();

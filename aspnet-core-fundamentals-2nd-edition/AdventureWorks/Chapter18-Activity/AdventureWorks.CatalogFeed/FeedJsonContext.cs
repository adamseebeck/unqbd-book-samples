using System.Text.Json.Serialization;

namespace AdventureWorks.CatalogFeed;

[JsonSerializable(typeof(FeedItem[]))]
internal sealed partial class FeedJsonContext
    : JsonSerializerContext;

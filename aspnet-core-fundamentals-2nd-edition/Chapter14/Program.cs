var builder = WebApplication.CreateBuilder(args);
bool useOpenApi31 = builder.Configuration.GetValue<bool>("OpenApi:Use31");

builder.Services.AddOpenApi("v1", options =>
{
    if (useOpenApi31)
        options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Commerce Catalog API";
        document.Info.Version = "1.0";
        return Task.CompletedTask;
    });
});
builder.Services.AddOpenApi("v2", options =>
{
    if (useOpenApi31)
        options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Commerce Catalog API";
        document.Info.Version = "2.0";
        return Task.CompletedTask;
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

var v1 = app.MapGroup("/api/v1/products");
v1.MapGet("/", () => TypedResults.Ok(new ProductV1[]
{
    new(101, "Notebook")
})).WithName("ListProductsV1").WithGroupName("v1");

var v2 = app.MapGroup("/api/v2/products");
v2.MapGet("/", () => TypedResults.Ok(new ProductV2[]
{
    new(101, "Notebook", 12.50m)
})).WithName("ListProductsV2").WithGroupName("v2");

app.Run();

public sealed record ProductV1(int Id, string Name);
public sealed record ProductV2(int Id, string Name, decimal UnitPrice);

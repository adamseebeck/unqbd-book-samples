var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.Configure<RouteOptions>(options =>
    options.ConstraintMap.Add("productCategory", typeof(ProductCategoryConstraint)));
builder.Services.Configure<RouteOptions>(options => options.ConstraintMap.Add("skuFormat", typeof(SkuFormatConstraint)));
var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.Use(async (context, next) =>
{
    var name = context.GetEndpoint()?.DisplayName;
    app.Logger.LogDebug("Selected endpoint {Endpoint}", name);
    await next();
});
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapProductEndpoints();
app.MapGet("/examples/orders/{pageNumber:int?}", (int? pageNumber) =>
    Results.Ok(new { Page = pageNumber ?? 1 }));
app.MapGet("/examples/status/{status:alpha=active}", (string status) =>
    Results.Ok(new { Status = status }));
app.MapGet("/examples/files/{**path}", (string? path) => Results.Ok(new { Path = path }));
app.MapGet("/examples/unconstrained/{id}", (int id) => Results.Ok(new { Id = id }));
app.MapGet("/examples/reviews/{id:int:min(1)}/{reviewId:guid}", (int id, Guid reviewId) =>
    Results.Ok(new { Id = id, ReviewId = reviewId }));
app.MapGet("/inventory/{sku:skuFormat}", (string sku) => Results.Ok(new { Sku = sku }));
var activityProducts = app.MapGroup("/api/products").WithTags("Product Management");
activityProducts.MapGet("/", () => Results.Ok(new[] { 1, 2 })).WithName("ActivityListProducts");
activityProducts.MapGet("/{id:int}", (int id) => Results.Ok(new { Id = id })).WithName("ActivityGetProduct");
activityProducts.MapPost("/", () => Results.CreatedAtRoute("ActivityGetProduct", new { id = 3 }, new { Id = 3 }));
activityProducts.MapGet("/{id:int}/related", (int id, LinkGenerator links, HttpContext context) =>
{
    var related = new[] { id + 1, id + 2 }.Select(value => links.GetPathByName(context, "ActivityGetProduct", new { id = value })).ToArray();
    return related.Any(path => path is null) ? Results.Problem("Exercise link generation failed.") : Results.Ok(related);
});
app.Run();

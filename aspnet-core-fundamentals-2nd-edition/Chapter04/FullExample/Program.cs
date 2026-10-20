var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.Configure<RouteOptions>(options =>
    options.ConstraintMap.Add("productCategory", typeof(ProductCategoryConstraint)));
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
app.Run();

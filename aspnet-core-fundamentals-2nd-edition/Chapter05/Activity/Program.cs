var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddControllers(options => options.Filters.Add<RequestLoggingFilter>());
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<OrderStore>();
var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.MapProductEndpointsV2();
app.MapGet("/api/binding/{id:int}", (int id, [Microsoft.AspNetCore.Mvc.FromQuery] string? q,
    [Microsoft.AspNetCore.Mvc.FromHeader(Name = "X-Api-Version")] string version) =>
    Results.Ok(new { Id = id, Query = q, Version = version }));
app.MapOrderEndpoints();
app.Run();

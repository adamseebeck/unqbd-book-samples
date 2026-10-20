using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] =
        Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default));
builder.Services.AddSingleton<IOrderService, OrderService>();
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOrderEndpoints();
app.Run();

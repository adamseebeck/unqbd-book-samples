using Microsoft.AspNetCore.Mvc;

// No token middleware: only the .NET 11 Sec-Fetch-Site and Origin
// check protects this form endpoint.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddPolicy("store",
    policy => policy.WithOrigins("https://localhost:7001")
        .AllowCredentials().AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();
app.MapPost("/newsletter", ([FromForm] string email) =>
    Results.Ok(new { email })).RequireCors("store");
app.Run();

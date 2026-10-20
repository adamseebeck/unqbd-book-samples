var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();
builder.Services.AddSingleton<ProductStore>();
builder.Services.AddProblemDetails();
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseAntiforgery();
app.MapRazorPages();
app.Run();

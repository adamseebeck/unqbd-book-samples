using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using AdventureWorks.AuthorizationSample;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<DocumentRepository>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    // This API training host returns status codes instead of login-page redirects.
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy("AtLeast21", policy => policy.RequireAuthenticatedUser().AddRequirements(new MinimumAgeRequirement(21)))
    .AddPolicy("DocumentOwner", policy => policy.RequireAuthenticatedUser().AddRequirements(new DocumentOwnerRequirement()));
builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, DocumentOwnerHandler>();
builder.Services.AddScoped<IAuthorizationHandler, DepartmentHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPolicyProvider>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
    Results.Ok(new { Token = antiforgery.GetAndStoreTokens(context).RequestToken })).RequireAuthorization();
app.MapGet("/", () => Results.Ok("Public AdventureWorks documentation")).AllowAnonymous();
app.MapGet("/restricted-content", () => Results.Ok("Age policy accepted")).RequireAuthorization("AtLeast21");
app.MapGet("/api/documents/{id:int}", async (int id, DocumentRepository repository,
    IAuthorizationService authorization, ClaimsPrincipal user) =>
{
    var document = repository.Find(id);
    if (document is null) return Results.NotFound();
    var result = await authorization.AuthorizeAsync(user, document, "DocumentOwner");
    return result.Succeeded ? Results.Ok(document) : Results.Forbid();
});
app.MapDelete("/api/documents/{id:int}", async (int id, DocumentRepository repository,
    HttpContext context, IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    return repository.Delete(id) ? Results.NoContent() : Results.NotFound();
})
    .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
// Activity: finance membership AND ownership must both succeed for a write.
app.MapPost("/api/documents/{id:int}/archive", async (int id, DocumentRepository repository,
    IAuthorizationService authorization, ClaimsPrincipal user, HttpContext context, IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    var document = repository.Find(id);
    if (document is null) return Results.NotFound();
    if (!(await authorization.AuthorizeAsync(user, document, "DocumentOwner")).Succeeded) return Results.Forbid();
    return repository.Archive(id) ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization("Department:Finance");
app.Run();

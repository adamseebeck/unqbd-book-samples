using System.Security.Claims;
using AdventureWorks.IdentitySample;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("This local credential-issuer example runs only in Development.");

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<DevelopmentTokenIssuer>();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("Identity") ?? "Data Source=identity-training.db"));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 12;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
});
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(5));
builder.Services.AddAuthentication().AddJwtBearer("Bearer");
builder.Services.AddOptions<JwtBearerOptions>("Bearer")
    .Configure<DevelopmentTokenIssuer>((options, issuer) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = DevelopmentTokenIssuer.Issuer,
            ValidateAudience = true, ValidAudience = DevelopmentTokenIssuer.Audience,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(issuer.Key),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddScoped<IClaimsTransformation, LibraryClaimsTransformation>();
builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync(); // Disposable training database only.
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var password = app.Configuration["Training:Password"]
        ?? throw new InvalidOperationException("Set Training__Password for the local account.");
    if (await users.FindByEmailAsync("learner@example.test") is null)
    {
        var result = await users.CreateAsync(new ApplicationUser
        { UserName = "learner@example.test", Email = "learner@example.test", LibraryCardNumber = "AW-001" }, password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapGet("/api/books/available", (ClaimsPrincipal user) =>
    Results.Ok(new { User = user.Identity?.Name, Books = new[] { "ASP.NET Core Fundamentals" } }))
    .RequireAuthorization(policy => policy.AddAuthenticationSchemes("Bearer").RequireAuthenticatedUser());
app.MapPost("/api/auth/login", async (LoginRequest request, UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn, ApplicationDbContext db, DevelopmentTokenIssuer issuer, TimeProvider clock) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 256 ||
        string.IsNullOrEmpty(request.Password) || request.Password.Length > 1024) return Results.Unauthorized();
    var user = await users.FindByEmailAsync(request.Email);
    if (user is null || !(await signIn.CheckPasswordSignInAsync(user, request.Password, true)).Succeeded)
        return Results.Unauthorized();
    var token = DevelopmentTokenIssuer.NewRefreshToken();
    db.RefreshTokens.Add(new RefreshToken { TokenHash = DevelopmentTokenIssuer.Hash(token),
        UserId = user.Id, Expires = clock.GetUtcNow().AddDays(7) });
    await db.SaveChangesAsync();
    return Results.Ok(new { AccessToken = issuer.Issue(user), RefreshToken = token });
});
app.MapPost("/api/auth/refresh", async (RefreshRequest request, ApplicationDbContext db,
    UserManager<ApplicationUser> users, DevelopmentTokenIssuer issuer, TimeProvider clock, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 256) return Results.Unauthorized();
    var hash = DevelopmentTokenIssuer.Hash(request.Token);
    var old = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
    if (old is null || old.ConsumedByHash is not null || old.Expires <= clock.GetUtcNow()) return Results.Unauthorized();
    var user = await users.FindByIdAsync(old.UserId);
    if (user is null || await users.IsLockedOutAsync(user)) return Results.Unauthorized();
    var raw = DevelopmentTokenIssuer.NewRefreshToken();
    old.ConsumedByHash = DevelopmentTokenIssuer.Hash(raw);
    db.RefreshTokens.Add(new RefreshToken { TokenHash = old.ConsumedByHash,
        UserId = user.Id, Expires = clock.GetUtcNow().AddDays(7) });
    try { await db.SaveChangesAsync(ct); }
    catch (DbUpdateConcurrencyException) { return Results.Unauthorized(); }
    return Results.Ok(new { AccessToken = issuer.Issue(user), RefreshToken = raw });
});
app.Run();

using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AdventureWorks;

public static class AccountServices
{
    // Visual Studio's ASP.NET and data workloads install LocalDB.
    // To use another server, set ConnectionStrings:SqlServer.
    const string LocalDb = @"Server=(localdb)\MSSQLLocalDB;"
        + "Database=AdventureWorksStore;Trusted_Connection=True";

    public static IServiceCollection AddAccountServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // A relative SQLite file lives in the project folder.
        var database = new SqliteConnectionStringBuilder(
            configuration.GetConnectionString("AdventureWorks"));
        database.DataSource = Path.Combine(
            environment.ContentRootPath, database.DataSource);
        services.AddDbContext<AdventureWorksDbContext>(options =>
        {
            if (configuration["Database:Provider"] == "SqlServer")
                options.UseSqlServer(
                    configuration.GetConnectionString("SqlServer")
                        ?? LocalDb,
                    sql => sql.MigrationsAssembly(
                        "AdventureWorks.SqlServerMigrations"));
            else
                options.UseSqlite(database.ConnectionString);
        });
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<TokenIssuer>();
        services.AddIdentity<ApplicationUser, IdentityRole>(
                ConfigureIdentity)
            .AddEntityFrameworkStores<AdventureWorksDbContext>()
            .AddDefaultTokenProviders();
        services.ConfigureApplicationCookie(ConfigureCookie);
        services.Configure<SecurityStampValidatorOptions>(o =>
            o.ValidationInterval = TimeSpan.FromMinutes(5));
        services.AddAuthentication()
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme);
        services.AddOptions<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme)
            .Configure<TokenIssuer>(ConfigureBearer);
        services.AddScoped<IClaimsTransformation,
            LoyaltyClaimsTransformation>();
        services.AddAuthorization();
        return services;
    }

    static void ConfigureIdentity(IdentityOptions options)
    {
        options.Password.RequiredLength = 12;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    }

    static void ConfigureCookie(
        CookieAuthenticationOptions options)
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        // Pages redirect to sign in; APIs answer 401 or 403.
        options.Events.OnRedirectToLogin = context =>
            Answer(context, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context =>
            Answer(context, StatusCodes.Status403Forbidden);
    }

    static Task Answer(
        RedirectContext<CookieAuthenticationOptions> context,
        int status)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = status;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    static void ConfigureBearer(
        JwtBearerOptions options, TokenIssuer issuer) =>
        options.TokenValidationParameters = new()
        {
            ValidIssuer = TokenIssuer.Issuer,
            ValidAudience = TokenIssuer.Audience,
            IssuerSigningKey = issuer.Key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30)
        };
}

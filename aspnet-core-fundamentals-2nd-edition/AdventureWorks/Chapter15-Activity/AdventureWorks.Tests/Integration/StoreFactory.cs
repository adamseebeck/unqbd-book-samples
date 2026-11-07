using System.Data;
using System.Net.Http.Json;
using System.Text.Json;
using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AdventureWorks.Tests.Integration;

// The whole app in memory, in the Testing environment, with two
// replacements: an in-memory SQLite database and a Test sign-in.
public sealed class StoreFactory : WebApplicationFactory<Program>
{
    public const string Customer = "test-customer";
    const string Test = TestAuthHandler.SchemeName;

    // The in-memory database lives as long as this connection.
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
        builder.ConfigureTestServices(services =>
        {
            // Remove the app's SQLite file registration first.
            services.RemoveAll<DbContextOptions<
                AdventureWorksDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<
                AdventureWorksDbContext>>();
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
            services.AddDbContext<AdventureWorksDbContext>(
                options => options.UseSqlite(_connection));
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions,
                    TestAuthHandler>(Test, null);
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme = Test;
                o.DefaultAuthenticateScheme = Test;
                o.DefaultChallengeScheme = Test;
                o.DefaultForbidScheme = Test;
            });
        });
    }

    // Build the schema from the migrations and seed what the
    // tests need: the catalog and one customer account. A factory
    // made with WithWebHostBuilder runs this again, so it checks
    // before it adds.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<AdventureWorksDbContext>();
        db.Database.Migrate();
        if (!db.Users.Any(u => u.Id == Customer))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = Customer,
                UserName = "tester@adventure-works.test",
                Email = "tester@adventure-works.test"
            });
            db.SaveChanges();
        }
        StoreSeed.AddCatalogAsync(db).GetAwaiter().GetResult();
        return host;
    }

    // A client signed in as the test customer, over HTTPS, with
    // the antiforgery header that order changes require.
    public async Task<HttpClient> CreateCustomerAsync(
        CancellationToken ct)
    {
        var client = CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add(
            TestAuthHandler.Header, Customer);
        var csrf = await client.GetFromJsonAsync<JsonElement>(
            "/api/csrf", ct);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",
            csrf.GetProperty("token").GetString());
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

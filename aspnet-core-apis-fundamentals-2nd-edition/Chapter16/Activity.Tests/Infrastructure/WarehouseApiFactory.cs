using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarehouseApi.Integration;
using WarehouseApi.Persistence;

namespace WarehouseApi.Tests.Infrastructure;

// One factory, one host and one SQL Server database for each test class that uses it as a class fixture
public class WarehouseApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string ConnectionString { get; } = TestDatabase.CreateConnectionString();

    public FakeShipmentHandler Erp { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development: no seeding at startup and no OpenAPI documents, as in production
        builder.UseEnvironment("Testing");

        // Development checks service lifetimes at startup (Chapter 3); Testing does not unless asked
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        // Program.cs reads these settings while it registers services; UseSetting supplies them in time
        builder.UseSetting("ConnectionStrings:WarehouseDb", ConnectionString);
        builder.UseSetting("Authentication:Schemes:Bearer:Authority", "https://login.example.com/warehouse/");
        builder.UseSetting("Authentication:Schemes:Bearer:ValidAudiences:0", "api://warehouse-api");
        builder.UseSetting("ExternalShipmentService:BaseUrl", "https://erp.example.com/");
        builder.UseSetting("ExternalShipmentService:ApiKey", "test-key");
        builder.UseSetting("ExternalShipmentService:Resilience:Retry:Delay", "00:00:00.050");

        builder.ConfigureTestServices(services =>
        {
            // The X-Test-* headers replace JWT bearer tokens; the authorization policies stay the same
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);

            // The ERP client keeps its resilience handler, but its requests reach the fake, not the network
            services.AddHttpClient<IExternalShipmentService, ExternalShipmentService>()
                .ConfigurePrimaryHttpMessageHandler(() => Erp);
        });
    }

    public WarehouseDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<WarehouseDbContext>().UseSqlServer(ConnectionString).Options);

    // xUnit runs this before the first test of the class: the database exists before the host starts,
    // so the fulfillment processor finds it when it looks for pending shipments
    public async Task InitializeAsync()
    {
        await using var context = CreateDbContext();
        await DbInitializer.SeedAsync(context);
    }

    // After the last test: stop the host and its background services, then drop the database
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await TestDatabase.DropAsync(ConnectionString);
    }
}

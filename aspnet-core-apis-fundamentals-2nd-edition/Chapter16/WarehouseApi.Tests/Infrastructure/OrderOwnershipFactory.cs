using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace WarehouseApi.Tests.Infrastructure;

// The normal test host, with a handler that can also represent a missing subject.
public class OrderOwnershipFactory : WarehouseApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.AddAuthentication(SubjectTestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, SubjectTestAuthHandler>(
                    SubjectTestAuthHandler.SchemeName, _ => { }));
    }

    public HttpClient CreateCaller(string roles, string? subject, string? warehouse = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(SubjectTestAuthHandler.RolesHeader, roles);
        if (subject is not null)
        {
            client.DefaultRequestHeaders.Add(SubjectTestAuthHandler.SubjectHeader,
                SubjectTestAuthHandler.SubjectPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(subject)));
        }

        if (warehouse is not null)
        {
            client.DefaultRequestHeaders.Add(SubjectTestAuthHandler.WarehouseHeader, warehouse);
        }

        return client;
    }
}

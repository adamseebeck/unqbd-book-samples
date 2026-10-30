using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Hosting;

namespace ContosoSupportCopilot.Infrastructure.Configuration;

public static class AzureCredentialFactory
{
    // One specific credential per environment: your Azure CLI sign-in (az login) on a
    // development machine, and the app's system-assigned managed identity when deployed.
    public static TokenCredential Create(IHostEnvironment environment) =>
        environment.IsDevelopment()
            ? new AzureCliCredential()
            : new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
}

using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Hosting;

namespace ContosoSupportCopilot.Infrastructure.Configuration;

public static class AzureCredentialFactory
{
    // One specific credential per environment, never a chain that tries several:
    // your Azure CLI sign-in (az login) on a development machine; in AKS, the workload identity
    // that the webhook injects (AZURE_CLIENT_ID, AZURE_TENANT_ID and AZURE_FEDERATED_TOKEN_FILE);
    // anywhere else in Azure, the app's system-assigned managed identity.
    public static TokenCredential Create(IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return new AzureCliCredential();
        }

        return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_FEDERATED_TOKEN_FILE"))
            ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
            : new WorkloadIdentityCredential();
    }
}

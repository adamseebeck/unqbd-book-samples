using System.Security;
using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Domain.Helpers;

public static class AccountContextHelper
{
    public static string FormatAccountContext(CustomerAccount? account)
    {
        if (account == null)
        {
            return "<customer_context>Unknown Customer, Tier: Unknown</customer_context>";
        }

        var tier = account.Tier switch
        {
            "Enterprise" => "Enterprise",
            "Professional" => "Professional",
            "Basic" => "Basic",
            _ => "Standard"
        };

        // The name is customer data, so it is escaped; the tier is one of the fixed values above.
        return $"<customer_context>{SecurityElement.Escape(account.Name)}, Tier: {tier}</customer_context>";
    }
}

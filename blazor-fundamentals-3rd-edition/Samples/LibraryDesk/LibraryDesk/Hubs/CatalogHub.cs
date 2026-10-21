using Microsoft.AspNetCore.SignalR;

namespace LibraryDesk.Hubs;

public sealed class CatalogHub : Hub
{
    public async Task Announce(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 120)
            throw new HubException("Use a message from 1 to 120 characters.");
        await Clients.All.SendAsync("CatalogNotice", text.Trim(), Context.ConnectionAborted);
    }
}

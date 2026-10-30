using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace WarehouseApi.Services;

public class IdempotencyContext
{
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _entries = new();
    private readonly TimeSpan _expiration = TimeSpan.FromHours(24);

    public bool TryGetEntry(string key, [NotNullWhen(true)] out IdempotencyEntry? entry)
    {
        if (_entries.TryGetValue(key, out entry))
        {
            if (DateTime.UtcNow - entry.Timestamp <= _expiration)
            {
                return true;
            }

            // Removes the expired entry only if no other request has replaced it
            _entries.TryRemove(KeyValuePair.Create(key, entry));
        }

        entry = null;
        return false;
    }

    public void Store(string key, object response, int statusCode, string requestHash)
    {
        _entries[key] = new IdempotencyEntry(response, statusCode, requestHash, DateTime.UtcNow);
    }

    public string CreateRequestHash(string requestSummary)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(requestSummary));
        return Convert.ToBase64String(bytes);
    }
}

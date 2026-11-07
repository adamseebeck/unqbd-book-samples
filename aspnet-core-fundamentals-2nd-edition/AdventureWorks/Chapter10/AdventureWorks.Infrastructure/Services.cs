using System.Collections.Concurrent;
using System.Net.Mail;
using AdventureWorks.Core;

namespace AdventureWorks.Infrastructure;

public sealed class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, string> _values
        = new();

    public string? Get(string key) =>
        _values.TryGetValue(key, out var value) ? value : null;

    public void Set(string key, string value) =>
        _values[key] = value;
}

public sealed class EmailValidator : IEmailValidator
{
    public bool IsValid(string address) =>
        !string.IsNullOrWhiteSpace(address) &&
        MailAddress.TryCreate(address, out var parsed) &&
        string.Equals(parsed.Address, address,
            StringComparison.OrdinalIgnoreCase);
}

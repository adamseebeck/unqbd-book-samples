using AdventureWorks.Core;
using AdventureWorks.Pages.Customer;

namespace AdventureWorks;

// Fictional local data for the Activity: one fixed customer.
// Real profile pages read the signed-in user (Chapter 8).
public sealed class CustomerProfileStore
{
    private readonly Lock _gate = new();
    private string _name = "Example Customer";
    private string _email = "customer@example.com";
    private bool _active = true;

    public Task<ProfileInput> ReadAsync()
    {
        lock (_gate)
            return Task.FromResult(new ProfileInput
            {
                Name = _name,
                Email = _email
            });
    }

    public Task SaveAsync(ProfileInput input)
    {
        lock (_gate) (_name, _email) = (input.Name, input.Email);
        return Task.CompletedTask;
    }

    public Task DeactivateAsync()
    {
        lock (_gate) _active = false;
        return Task.CompletedTask;
    }

    public bool IsActive { get { lock (_gate) return _active; } }

    public Order[] RecentOrders() =>
        [new(1, "AW-0001", 49.00m), new(2, "AW-0002", 189.00m)];
}

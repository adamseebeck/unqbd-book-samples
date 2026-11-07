using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AdventureWorks.Infrastructure.Identity;

// A store customer or staff member. The loyalty number lives in
// the Identity store, never in a sign-in request.
public sealed class ApplicationUser : IdentityUser
{
    [MaxLength(20)]
    public string LoyaltyNumber { get; set; } = "";
}

// Only a SHA-256 hash of each refresh token is stored.
public sealed class RefreshToken
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string TokenHash { get; set; } = "";

    public string UserId { get; set; } = "";

    public DateTimeOffset Expires { get; set; }

    [ConcurrencyCheck, MaxLength(64)]
    public string? ConsumedByHash { get; set; }
}

using ContosoSupportCopilot.Domain.Models;

namespace ContosoSupportCopilot.Infrastructure.Data;

/// <summary>Fixed sample tickets and articles so every run starts from the same data.</summary>
public static class SampleData
{
    public static readonly Guid PasswordTicketId = new("3f2b8c1e-0001-4a6b-9a1e-000000000001");
    public static readonly Guid PaymentsTicketId = new("3f2b8c1e-0002-4a6b-9a1e-000000000002");
    public static readonly Guid ApiTicketId = new("3f2b8c1e-0003-4a6b-9a1e-000000000003");

    private static readonly DateTime Start = new(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc);

    public static List<SupportTicket> CreateTickets() =>
    [
        new SupportTicket
        {
            Id = PasswordTicketId,
            Subject = "Cannot sign in after password reset",
            Description = "I reset my password this morning and now every sign in attempt says my account is locked. Our team is blocked until I can sign in.",
            Priority = "High",
            ProductArea = "Authentication",
            CreatedAt = Start,
            Customer = new CustomerAccount { Id = new("7d1c0b2a-0001-4c3d-8e5f-000000000001"), Name = "Fabrikam", Tier = "Enterprise" },
            Messages =
            [
                new TicketMessage { Id = new("5a6b7c8d-0001-4e0f-9a1b-000000000001"), Content = "Every sign in attempt says my account is locked.", IsFromCustomer = true, SentAt = Start },
                new TicketMessage { Id = new("5a6b7c8d-0002-4e0f-9a1b-000000000002"), Content = "Thanks for the report. Are you using the new password on the web portal?", IsFromCustomer = false, SentAt = Start.AddMinutes(20) },
                new TicketMessage { Id = new("5a6b7c8d-0003-4e0f-9a1b-000000000003"), Content = "Yes, and the mobile app shows the same message.", IsFromCustomer = true, SentAt = Start.AddMinutes(35) }
            ]
        },
        new SupportTicket
        {
            Id = PaymentsTicketId,
            Subject = "Charged twice for the October invoice",
            Description = "Our card was charged twice for the October invoice. Please refund the duplicate payment.",
            ProductArea = "Payments",
            CreatedAt = Start.AddHours(2),
            Customer = new CustomerAccount { Id = new("7d1c0b2a-0002-4c3d-8e5f-000000000002"), Name = "Northwind Traders", Tier = "Professional" },
            Messages =
            [
                new TicketMessage { Id = new("5a6b7c8d-0004-4e0f-9a1b-000000000004"), Content = "We see two identical charges on October 3.", IsFromCustomer = true, SentAt = Start.AddHours(2) }
            ]
        },
        new SupportTicket
        {
            Id = ApiTicketId,
            Subject = "API returns 429 during nightly sync",
            Description = "Our nightly integration job gets HTTP 429 responses from the orders endpoint after about 200 requests. How should we handle the rate limit?",
            ProductArea = "API",
            CreatedAt = Start.AddHours(5),
            Customer = new CustomerAccount { Id = new("7d1c0b2a-0003-4c3d-8e5f-000000000003"), Name = "Tailspin Toys", Tier = "Basic" },
            Messages =
            [
                new TicketMessage { Id = new("5a6b7c8d-0005-4e0f-9a1b-000000000005"), Content = "The job starts at 01:00 UTC and fails after a few minutes.", IsFromCustomer = true, SentAt = Start.AddHours(5) }
            ]
        }
    ];

    public static List<KnowledgeArticle> CreateArticles() =>
    [
        new KnowledgeArticle
        {
            Id = new("9e8d7c6b-0001-4a5b-8c7d-000000000001"),
            Title = "Restore access after a password reset",
            Content = "If a user is locked out after a password reset, an administrator can clear the lockout from the user page. Ask the user to sign out of every device first, because old sessions retry the previous password and lock the account again.",
            ProductArea = "Authentication",
            Tags = ["password", "lockout"],
            LastUpdated = Start.AddDays(-30),
            IsPublished = true
        },
        new KnowledgeArticle
        {
            Id = new("9e8d7c6b-0002-4a5b-8c7d-000000000002"),
            Title = "Refund a duplicate charge",
            Content = "Duplicate charges usually come from a retried payment. Confirm both transactions in the payments dashboard, refund the later one, and tell the customer that the refund takes three to five business days.",
            ProductArea = "Payments",
            Tags = ["refund", "invoice"],
            LastUpdated = Start.AddDays(-12),
            IsPublished = true
        },
        new KnowledgeArticle
        {
            Id = new("9e8d7c6b-0003-4a5b-8c7d-000000000003"),
            Title = "Handle API rate limits",
            Content = "The orders endpoint allows 120 requests per minute per key. When a response returns HTTP 429, wait for the number of seconds in the Retry-After header, then retry with exponential backoff.",
            ProductArea = "API",
            Tags = ["rate limit", "429"],
            LastUpdated = Start.AddDays(-5),
            IsPublished = true
        }
    ];
}

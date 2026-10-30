using ContosoSupportCopilot.Domain.Evaluation;

namespace ContosoSupportCopilot.Infrastructure.Data;

/// <summary>Questions with known answers and known relevant articles, written from support ticket history.</summary>
public static class SampleEvaluationData
{
    public static EvaluationDataset CreateDataset()
    {
        var dataset = new EvaluationDataset { Name = "support-knowledge-base" };

        dataset.AddTestCase(
            "password-lockout",
            "The customer is locked out after a password reset",
            "Authentication",
            "Ask the user to sign out of every device, then clear the lockout from the user page.",
            [SampleData.RestoreAccessArticleId, SampleData.SignInErrorsArticleId]);

        dataset.AddTestCase(
            "reset-link-expiry",
            "How long does a reset link stay valid?",
            "Authentication",
            "Reset links expire after one hour.",
            [SampleData.SignInErrorsArticleId]);

        dataset.AddTestCase(
            "duplicate-charge",
            "When does the customer get the refund for a duplicate charge?",
            "Payments",
            "Refund the later charge; the refund takes three to five business days.",
            [SampleData.DuplicateChargeArticleId]);

        dataset.AddTestCase(
            "rate-limit",
            "Our job gets HTTP 429 responses from the orders endpoint",
            "API",
            "Wait for the seconds in the Retry-After header, then retry with exponential backoff.",
            [SampleData.RateLimitArticleId]);

        // A question the knowledge base cannot answer: the right result is a refusal, not a guess.
        dataset.AddTestCase(
            "out-of-scope",
            "How do I export a dashboard to PDF?",
            "Reporting",
            "The knowledge base has no article about dashboard exports.",
            []);

        return dataset;
    }
}

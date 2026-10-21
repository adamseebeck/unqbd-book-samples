using Bunit;
using Xunit;
public sealed class InvestigationTests
{
    [Fact] public void TenCharacterMinimumChangesTheActualFormDecision()
    {
        using var context = new BunitContext();
        var cut = context.Render<LibraryDesk.Client.Components.FeedbackForm>();
        cut.Find("#feedback-name").Change("Ada"); cut.Find("#feedback-email").Change("ada@example.test");
        cut.Find("#feedback-message").Change("123456789"); cut.Find("form").Submit();
        Assert.Contains("Nothing saved.", cut.Markup); Assert.NotEmpty(cut.FindAll(".validation-message"));
        cut.Find("#feedback-message").Change("1234567890"); cut.Find("form").Submit();
        Assert.Contains("Feedback accepted locally for Ada", cut.Markup);
    }
}

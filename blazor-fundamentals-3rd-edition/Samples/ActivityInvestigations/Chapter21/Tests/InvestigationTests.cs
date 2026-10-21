using Bunit;
using Xunit;
public sealed class InvestigationTests
{
    [Fact] public void HelpHasAValidRelationshipAndValidationStillWorks()
    {
        using var context = new BunitContext();
        var cut = context.Render<LibraryDesk.Client.Components.FeedbackForm>();
        Assert.Equal("feedback-message-help feedback-message-error", cut.Find("textarea").GetAttribute("aria-describedby"));
        Assert.Equal("feedback-message", cut.Find("label[for='feedback-message']").GetAttribute("for"));
        Assert.Contains("5 to 300 characters", cut.Find("#feedback-message-help").TextContent);
        cut.Find("form").Submit(); Assert.NotEmpty(cut.FindAll(".validation-message"));
        cut.Find("#feedback-name").Change("Ada"); cut.Find("#feedback-email").Change("ada@example.test");
        cut.Find("#feedback-message").Change("A useful local example."); cut.Find("form").Submit();
        Assert.Contains("Feedback accepted locally for Ada", cut.Markup);
    }
}

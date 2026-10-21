using Bunit;
using LibraryDesk.Client.Components;
using Xunit;
public sealed class AccessibilityTests
{
    [Fact]
    public void FieldErrorsHaveProgrammaticRelationships()
    {
        using var context = new BunitContext();
        var cut = context.Render<FeedbackForm>();
        cut.Find("form").Submit();
        foreach (var name in new[] { "name", "email", "message" })
        {
            var id = "feedback-" + name;
            var field = cut.Find("#" + id);
            Assert.Equal(id + "-error", field.GetAttribute("aria-describedby"));
            Assert.Equal("true", field.GetAttribute("aria-required"));
            Assert.Equal("true", field.GetAttribute("aria-invalid"));
            Assert.NotEmpty(cut.Find("#" + id + "-error .validation-message").TextContent);
            Assert.Equal(id, cut.Find("label[for='" + id + "']").GetAttribute("for"));
        }
    }
}

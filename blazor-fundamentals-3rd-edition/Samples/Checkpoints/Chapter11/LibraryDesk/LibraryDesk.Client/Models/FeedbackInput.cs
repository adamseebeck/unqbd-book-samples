using System.ComponentModel.DataAnnotations;

namespace LibraryDesk.Client.Models;

public sealed class FeedbackInput
{
    [Required, StringLength(60)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, StringLength(120)]
    public string Email { get; set; } = "";

    [Required, StringLength(300, MinimumLength = 5)]
    public string Message { get; set; } = "";
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdventureWorks.Pages.Customer;

public sealed class ProfileInput
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class ProfileModel(CustomerProfileStore store)
    : PageModel
{
    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync() =>
        Input = await store.ReadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await store.SaveAsync(Input);
        StatusMessage = "Profile saved.";
        return RedirectToPage();
    }

    // A named handler: it reads none of the edit form's fields.
    public async Task<IActionResult> OnPostDeactivateAsync()
    {
        await store.DeactivateAsync();
        return RedirectToPage("./Deactivated");
    }
}

using System.ComponentModel.DataAnnotations;
using AdventureWorks.IdentitySample;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace AdventureWorks.IdentitySample.Pages.Account;

public sealed class LoginModel(SignInManager<ApplicationUser> signIn) : PageModel
{
    public sealed class LoginInput
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    }
    [BindProperty] public LoginInput Input { get; set; } = new();
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();
        var result = await signIn.PasswordSignInAsync(Input.Email, Input.Password, false, true);
        if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Account/Profile");
        ModelState.AddModelError("", "Invalid login attempt.");
        return Page();
    }
    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await signIn.SignOutAsync();
        return RedirectToPage("Login");
    }
}

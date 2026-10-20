using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
public sealed class ProfileInput
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}
public sealed record FictionalOrder(int Id, decimal Total);
public sealed class FictionalCustomerStore
{
    private readonly object _gate = new();
    private ProfileInput _input = new() { Name = "Example Customer", Email = "customer@example.com" };
    private bool _active = true;
    public Task<ProfileInput> ReadAsync()
    {
        lock (_gate) return Task.FromResult(new ProfileInput { Name = _input.Name, Email = _input.Email });
    }
    public Task SaveAsync(ProfileInput input)
    {
        lock (_gate) _input = new ProfileInput { Name = input.Name, Email = input.Email };
        return Task.CompletedTask;
    }
    public Task DeactivateAsync() { lock (_gate) _active = false; return Task.CompletedTask; }
    public bool IsActive { get { lock (_gate) return _active; } }
    public FictionalOrder[] RecentOrders() => [new(101, 25m), new(102, 40m)];
}
public sealed class ProfileModel(FictionalCustomerStore store) : PageModel
{
    [BindProperty] public ProfileInput Input { get; set; } = new();
    [TempData] public string? StatusMessage { get; set; }
    public async Task OnGetAsync() => Input = await store.ReadAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await store.SaveAsync(Input);
        StatusMessage = "Profile saved.";
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeactivateAsync()
    {
        await store.DeactivateAsync();
        return RedirectToPage("./Deactivated");
    }
}
public sealed class RecentOrdersViewComponent(FictionalCustomerStore store) : ViewComponent
{
    public IViewComponentResult Invoke() => View(store.RecentOrders().Take(5).ToArray());
}

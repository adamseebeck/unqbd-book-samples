using Chapter07Sample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Chapter07Sample.Pages.Orders;

[RequestSizeLimit(2 * 1024 * 1024)]
public sealed class CreateModel(IWebHostEnvironment environment) : PageModel
{
    [BindProperty]
    public OrderFormInput Input { get; set; } = new();
    [BindProperty]
    public IFormFile? Upload { get; set; }
    public string? StatusMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Upload is not null && (Upload.Length == 0 || Upload.Length > 1024 * 1024))
            ModelState.AddModelError(nameof(Upload), "Choose a nonempty file up to 1 MiB.");
        if (!ModelState.IsValid)
            return Page();

        if (Upload is not null)
        {
            string directory = Path.Combine(environment.ContentRootPath, "PrivateUploads");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".bin");
            bool created = false;
            try
            {
                await using var output = new FileStream(path, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None);
                created = true;
                await Upload.CopyToAsync(output, HttpContext.RequestAborted);
            }
            catch
            {
                if (created) System.IO.File.Delete(path);
                throw;
            }
        }
        StatusMessage = "Input validated. No order has been placed.";
        return Page();
    }
}

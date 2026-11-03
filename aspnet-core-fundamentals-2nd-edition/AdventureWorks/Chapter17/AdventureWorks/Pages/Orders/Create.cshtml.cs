using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdventureWorks.Pages.Orders;

[RequestSizeLimit(2 * 1024 * 1024)]
public sealed class CreateModel(IProductService products,
    IWebHostEnvironment environment) : PageModel
{
    const long MaxUploadBytes = 1024 * 1024;

    [BindProperty]
    public OrderFormInput Input { get; set; } = new();

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public string? StatusMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Upload is { Length: 0 or > MaxUploadBytes })
            ModelState.AddModelError(nameof(Upload),
                "Choose a nonempty file up to 1 MiB.");
        var product =
            await products.GetByIdAsync(Input.ProductId);
        if (product is null)
            ModelState.AddModelError("Input.ProductId",
                "No product has that ID.");
        if (!ModelState.IsValid) return Page();
        if (Upload is not null)
            await SavePrivateCopyAsync(Upload);
        StatusMessage = "Input validated for " +
            $"{Input.Quantity} x {product!.Name}. " +
            "No order has been placed.";
        return Page();
    }

    async Task SavePrivateCopyAsync(IFormFile upload)
    {
        var directory = Path.Combine(
            environment.ContentRootPath, "PrivateUploads");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(
            directory, Guid.NewGuid().ToString("N") + ".bin");
        var created = false;
        try
        {
            await using var output = new FileStream(path,
                FileMode.CreateNew, FileAccess.Write,
                FileShare.None);
            created = true;
            await upload.CopyToAsync(
                output, HttpContext.RequestAborted);
        }
        catch
        {
            if (created) System.IO.File.Delete(path);
            throw;
        }
    }
}

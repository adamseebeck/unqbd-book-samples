using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdventureWorks.Pages.Products;

public sealed class EditModel(IProductService products)
    : PageModel
{
    [BindProperty]
    public ProductInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var product = await products.GetByIdAsync(id);
        if (product is null) return NotFound();
        Input = new ProductInput
        {
            Name = product.Name,
            Price = product.Price,
            Version = product.Version
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (await products.GetByIdAsync(id) is null)
            return NotFound();
        if (!ModelState.IsValid) return Page();
        var changes = new UpdateProductRequest
        {
            Name = Input.Name,
            Price = Input.Price
        };
        var outcome = await products.UpdateAsync(
            id, changes, Input.Version);
        if (outcome == UpdateOutcome.NotFound) return NotFound();
        if (outcome == UpdateOutcome.Conflict)
        {
            ModelState.AddModelError(string.Empty,
                "This product changed. Reload before saving.");
            return Page();
        }
        StatusMessage = "Product updated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!await products.DeleteAsync(id)) return NotFound();
        StatusMessage = "Product deleted.";
        return RedirectToPage("./Index");
    }
}

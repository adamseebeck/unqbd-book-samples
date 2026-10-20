using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdventureWorks.Pages.Products;

public sealed class EditModel(ProductStore products) : PageModel
{
    [BindProperty]
    public ProductInput Input { get; set; } = new();
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var product = await products.GetAsync(id);
        if (product is null) return NotFound();
        Input = new ProductInput { Name = product.Name, Price = product.Price, Version = product.Version };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (await products.GetAsync(id) is null) return NotFound();
        if (!ModelState.IsValid) return Page();
        if (!await products.UpdateAsync(id, Input))
        {
            ModelState.AddModelError(string.Empty, "This product changed. Reload before saving again.");
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

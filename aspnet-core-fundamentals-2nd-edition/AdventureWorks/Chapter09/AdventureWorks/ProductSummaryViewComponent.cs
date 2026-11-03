using AdventureWorks.Core;
using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks;

public sealed class ProductSummaryViewComponent(
    IProductService products) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var visible = await products.SearchAsync(null, 1, 20);
        return View(visible.Length);
    }
}

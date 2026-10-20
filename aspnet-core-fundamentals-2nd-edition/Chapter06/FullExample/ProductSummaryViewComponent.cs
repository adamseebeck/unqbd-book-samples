using Microsoft.AspNetCore.Mvc;

public sealed class ProductSummaryViewComponent(ProductStore products) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var visible = await products.ListAsync();
        return View(visible.Length);
    }
}

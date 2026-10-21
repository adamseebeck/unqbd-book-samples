using MauiInventory.Core;

namespace MauiInventory.App;
public sealed class StockTemplateSelector : DataTemplateSelector
{
    // Keep the same template instances; do not create a new DataTemplate for each row.
    public DataTemplate Standard { get; set; } = null!;
    public DataTemplate LowStock { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) => item is Product { Quantity: < 10 } ? LowStock : Standard;
}

using Microsoft.AspNetCore.Mvc;

namespace AdventureWorks;

public sealed class RecentOrdersViewComponent(
    CustomerProfileStore store) : ViewComponent
{
    public IViewComponentResult Invoke() =>
        View(store.RecentOrders().Take(5).ToArray());
}

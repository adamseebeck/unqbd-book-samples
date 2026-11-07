namespace MauiInventory.App;
internal static class GridPlacement
{
    public static T AtRow<T>(this T view, int row)
        where T : View
    {
        Grid.SetRow(view, row);
        return view;
    }
}

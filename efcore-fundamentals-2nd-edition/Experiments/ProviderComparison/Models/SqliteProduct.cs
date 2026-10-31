public sealed class SqliteProduct
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public SqliteCategory Category { get; set; } = null!;
    public Guid Version { get; set; }
}

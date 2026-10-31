public sealed class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public List<ProductCategory> ProductCategories { get; set; }
        = [];
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
}

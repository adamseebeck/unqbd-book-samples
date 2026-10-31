public sealed class Supplier
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<Product> Products { get; set; } = [];
}

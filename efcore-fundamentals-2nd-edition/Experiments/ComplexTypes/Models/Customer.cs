public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Address ShippingAddress { get; set; } = new();
}

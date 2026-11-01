using Microsoft.EntityFrameworkCore;

public sealed class ComplexContext(
    DbContextOptions<ComplexContext> options)
    : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Customer>(customer =>
        {
            customer.Property(c => c.Name).HasMaxLength(100);
            customer.ComplexProperty(c => c.ShippingAddress,
                address =>
            {
                address.Property(a => a.Street).HasMaxLength(200);
                address.Property(a => a.PostalCode)
                    .HasMaxLength(20);
            });
        });
    }
}

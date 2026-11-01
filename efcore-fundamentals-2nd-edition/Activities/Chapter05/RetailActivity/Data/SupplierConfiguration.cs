using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SupplierConfiguration
    : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> supplier)
    {
        supplier.Property(s => s.CompanyName).HasMaxLength(150);
        supplier.Property(s => s.ContactEmail).HasMaxLength(255);
        supplier.HasIndex(s => s.ContactEmail).IsUnique();
        supplier.Property(s => s.PhoneNumber).HasMaxLength(20);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseApi.Models;

namespace WarehouseApi.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.CustomerReference).HasMaxLength(50).IsRequired();

        // Stored as text, such as "Allocated", rather than a number
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(o => o.FulfillmentWarehouseCode).HasMaxLength(10);
        builder.HasOne<Warehouse>().WithMany()
            .HasForeignKey(o => o.FulfillmentWarehouseCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Lines).WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(o => o.RowVersion).IsRowVersion();
    }
}

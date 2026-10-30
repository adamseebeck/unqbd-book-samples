using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseApi.Models;

namespace WarehouseApi.Persistence.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProductSku).HasMaxLength(20);
        builder.Property(s => s.WarehouseCode).HasMaxLength(10);

        // One stock item per product and warehouse; the included columns let the low-stock
        // query read every value it needs from this index, already in SKU order
        builder.HasIndex(s => new { s.WarehouseCode, s.ProductSku })
            .IsUnique()
            .IncludeProperties(s => new { s.OnHand, s.Reserved, s.ReorderThreshold });

        // Deleting a product or a warehouse must not silently delete stock
        builder.HasOne(s => s.Product).WithMany()
            .HasForeignKey(s => s.ProductSku)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Warehouse).WithMany()
            .HasForeignKey(s => s.WarehouseCode)
            .OnDelete(DeleteBehavior.Restrict);

        // The optimistic concurrency token: a rowversion column
        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

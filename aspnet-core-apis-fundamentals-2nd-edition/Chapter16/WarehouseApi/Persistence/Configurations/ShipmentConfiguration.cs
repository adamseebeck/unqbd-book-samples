using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseApi.Models;

namespace WarehouseApi.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        // One shipment per order: the order's key is also the shipment's key
        builder.HasKey(s => s.OrderId);
        builder.HasOne<Order>().WithOne()
            .HasForeignKey<Shipment>(s => s.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Stored as text, such as "Pending", like the order status
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(s => s.ShipmentId).HasMaxLength(50);
        builder.Property(s => s.TrackingNumber).HasMaxLength(50);
        builder.Property(s => s.LastError).HasMaxLength(Shipment.MaxErrorLength);
        builder.Property(s => s.CorrelationId).HasMaxLength(64).IsRequired();

        // The processor reads the pending shipments when it starts
        builder.HasIndex(s => s.Status);
    }
}

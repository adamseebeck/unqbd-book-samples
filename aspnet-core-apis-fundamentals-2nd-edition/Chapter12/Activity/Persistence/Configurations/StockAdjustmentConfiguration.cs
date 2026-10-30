using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseApi.Models;

namespace WarehouseApi.Persistence.Configurations;

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ReasonCode).HasMaxLength(20).IsRequired();

        // The order ID or the idempotency key behind the change, and the request's correlation ID
        builder.Property(a => a.ReferenceNumber).HasMaxLength(100);
        builder.Property(a => a.CorrelationId).HasMaxLength(64);

        // The history is the audit trail: a stock item that has history rows cannot be deleted
        builder.HasOne<StockItem>().WithMany()
            .HasForeignKey(a => a.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

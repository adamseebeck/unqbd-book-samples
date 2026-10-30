using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseApi.Models;

namespace WarehouseApi.Persistence.Configurations;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Sku).HasMaxLength(20);

        builder.HasOne<Product>().WithMany()
            .HasForeignKey(l => l.Sku)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

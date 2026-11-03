using AdventureWorks.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdventureWorks.Infrastructure.Data;

// Rules the database enforces, whatever code writes to it.
internal sealed class ProductConfiguration
    : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(
        EntityTypeBuilder<ProductEntity> product)
    {
        product.ToTable("Products", table =>
            table.HasCheckConstraint(
                "CK_Products_PriceCents", "PriceCents >= 0"));
        product.Property(p => p.Name).HasMaxLength(100);
        product.Property(p => p.Sku).HasMaxLength(8);
        product.Property(p => p.Category).HasMaxLength(20);
        product.HasIndex(p => p.Sku).IsUnique();
    }
}

internal sealed class OrderConfiguration
    : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> order)
    {
        order.ToTable("Orders");
        order.Property(o => o.OrderNumber).HasMaxLength(20);
        order.HasIndex(o => o.OrderNumber).IsUnique();
        order.Property(o => o.Status).HasMaxLength(20);
        order.Property(o => o.Notes).HasMaxLength(500);
        // Each UPDATE checks this value; see SaveChangesAsync.
        order.Property(o => o.Version).IsConcurrencyToken();
        order.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(o => o.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        order.HasMany(o => o.Items).WithOne()
            .HasForeignKey(i => i.OrderId);
    }
}

internal sealed class OrderItemConfiguration
    : IEntityTypeConfiguration<OrderItemEntity>
{
    public void Configure(EntityTypeBuilder<OrderItemEntity> item)
    {
        item.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint(
                "CK_OrderItems_Quantity", "Quantity > 0");
            table.HasCheckConstraint(
                "CK_OrderItems_UnitPriceCents",
                "UnitPriceCents >= 0");
        });
        // A product that is on an order cannot be deleted.
        item.HasOne<ProductEntity>().WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InventoryItemConfiguration
    : IEntityTypeConfiguration<InventoryItemEntity>
{
    public void Configure(
        EntityTypeBuilder<InventoryItemEntity> stock)
    {
        stock.ToTable("InventoryItems", table =>
            table.HasCheckConstraint(
                "CK_InventoryItems_Quantity", "Quantity >= 0"));
        stock.Property(s => s.Location).HasMaxLength(50);
        stock.HasIndex(s => new { s.ProductId, s.Location })
            .IsUnique();
        stock.HasOne<ProductEntity>().WithMany()
            .HasForeignKey(s => s.ProductId);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseApi.Models;

namespace WarehouseApi.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        // Stock items and orders refer to a warehouse by its code
        builder.HasKey(w => w.Code);
        builder.Property(w => w.Code).HasMaxLength(10);

        // The number in /api/warehouses/{id} stays unique, but it is not the key
        builder.HasIndex(w => w.Id).IsUnique();

        builder.Property(w => w.Name).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Location).HasMaxLength(200).IsRequired();
    }
}

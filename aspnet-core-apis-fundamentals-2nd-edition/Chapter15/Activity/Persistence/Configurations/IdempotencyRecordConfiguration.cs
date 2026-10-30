using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseApi.Contracts;
using WarehouseApi.Models;

namespace WarehouseApi.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        // The key is the primary key, so only one request can store it
        builder.HasKey(r => r.Key);

        // The longest key the endpoints accept. A binary collation compares keys exactly; the default
        // collation would treat abc and ABC as one key
        builder.Property(r => r.Key)
            .HasMaxLength(IdempotencyKeyRules.MaxLength)
            .UseCollation("Latin1_General_100_BIN2");

        // A Base64 SHA-256 hash is 44 characters long
        builder.Property(r => r.RequestHash).HasMaxLength(44).IsRequired();
        builder.Property(r => r.ResponseBody).IsRequired();
    }
}

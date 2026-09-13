using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Item", "dbo");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseIdentityColumn();

        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();

        builder.HasIndex(i => i.ProductId);
    }
}

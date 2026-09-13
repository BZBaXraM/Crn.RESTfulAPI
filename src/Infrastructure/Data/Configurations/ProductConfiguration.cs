using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product", "dbo");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityColumn();

        builder.Property(p => p.ProductName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.CreatedOn)
            .HasColumnType("datetime")
            .IsRequired();

        builder.Property(p => p.ModifiedBy)
            .HasMaxLength(100);

        builder.Property(p => p.ModifiedOn)
            .HasColumnType("datetime");

        builder.HasIndex(p => p.ProductName);

        builder.HasMany(p => p.Items)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken", "dbo");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityColumn();

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(64);
        builder.Property(t => t.ExpiresOn).HasColumnType("datetime").IsRequired();
        builder.Property(t => t.CreatedOn).HasColumnType("datetime").IsRequired();
        builder.Property(t => t.RevokedOn).HasColumnType("datetime");

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.IsRevoked);
    }
}

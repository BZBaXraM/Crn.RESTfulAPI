using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User", "dbo");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).UseIdentityColumn();

        builder.Property(u => u.UserName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(u => u.CreatedOn).HasColumnType("datetime").IsRequired();

        builder.HasIndex(u => u.UserName).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

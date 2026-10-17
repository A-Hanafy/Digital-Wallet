using DigitalWallet.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalWallet.Infrastructure.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Balance)
            .HasPrecision(18, 2);

        builder.Property(w => w.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.HasOne(w => w.User)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
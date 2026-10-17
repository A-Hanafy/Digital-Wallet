using DigitalWallet.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalWallet.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.ReferenceNumber)
            .IsRequired();

        // علاقة المحفظة المرسلة
        builder.HasOne(t => t.SenderWallet)
            .WithMany(w => w.SentTransactions)
            .HasForeignKey(t => t.SenderWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // علاقة المحفظة المستقبلة
        builder.HasOne(t => t.ReceiverWallet)
            .WithMany(w => w.ReceivedTransactions)
            .HasForeignKey(t => t.ReceiverWalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
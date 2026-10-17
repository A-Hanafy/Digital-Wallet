using DigitalWallet.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalWallet.Infrastructure.Data.Configurations;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.HasKey(i => i.Id);

        // جعل الـ Key فريد لمنع تكرار الطلبات
        builder.HasIndex(i => i.Key)
            .IsUnique();
    }
}
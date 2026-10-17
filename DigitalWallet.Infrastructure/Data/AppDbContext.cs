using DigitalWallet.Core.Entities;
using DigitalWallet.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
using DigitalWallet.Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalWallet.Core.Entities
{
    public class Wallet
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string UserId { get; set; } = string.Empty; 
        public decimal Balance { get; set; } = 0.00m;
        public string Currency { get; set; } = "EGP";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // خاصية منع الـ Concurrency Race Conditions
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        public virtual AppUser User { get; set; } = null!;
        public virtual ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
    }
}

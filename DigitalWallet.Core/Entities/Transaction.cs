using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWallet.Core.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? SenderWalletId { get; set; }   
        public Guid? ReceiverWalletId { get; set; }   
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public TransactionType Type { get; set; }
        public TransactionStatus Status { get; set; }
        public string ReferenceNumber { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Wallet? SenderWallet { get; set; }
        public virtual Wallet? ReceiverWallet { get; set; }
    }

    public enum TransactionType
    {
        Deposit = 1,
        Withdrawal = 2,
        Transfer = 3
    }

    public enum TransactionStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3
    }
}


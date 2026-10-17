using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWallet.Application.DTOs
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid? SenderWalletId { get; set; }
        public Guid? ReceiverWalletId { get; set; }
    }
}

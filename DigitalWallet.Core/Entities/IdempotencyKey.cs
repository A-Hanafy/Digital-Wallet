using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWallet.Core.Entities
{
    public class IdempotencyKey
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Key { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ResponseData { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

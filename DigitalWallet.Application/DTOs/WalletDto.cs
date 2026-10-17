namespace DigitalWallet.Application.DTOs
{
    public class WalletDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
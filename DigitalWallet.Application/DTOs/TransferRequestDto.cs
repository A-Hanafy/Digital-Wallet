using System.ComponentModel.DataAnnotations;

public class TransferRequestDto
{
    [Required]
    public Guid ReceiverWalletId { get; set; }

    [Required]
    [Range(0.01, 1000000, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code.")]
    public string Currency { get; set; } = "EGP";

    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;
}
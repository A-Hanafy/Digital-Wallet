using System.ComponentModel.DataAnnotations;

namespace DigitalWallet.Application.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression("(?=^.{6,}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&amp;*()_+([^_{}]|[\\u0020-\\u002F]|[\u003A-\u0040]|[\u005B-\u0060]|[\u007B-\u007E]).*$",
            ErrorMessage = "Password must have 1 uppercase, 1 lowercase, 1 number, 1 non alphanumeric and at least 6 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
using Microsoft.AspNetCore.Identity;


namespace DigitalWallet.Core.Entities.Identity
{
    public class AppUser :IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
        public Wallet? Wallet { get; set; }
    }
}

using DigitalWallet.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;


namespace DigitalWallet.Application.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(AppUser user, UserManager<AppUser> userManager);
    }
}

using DigitalWallet.Application.DTOs;
using DigitalWallet.Application.Interfaces;
using DigitalWallet.Core.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DigitalWallet.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null) return Unauthorized(new { message = "Invalid email or password" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded) return Unauthorized(new { message = "Invalid email or password" });

            return Ok(new UserDto
            {
                Email = user.Email!,
                DisplayName = user.DisplayName,
                Token = await _tokenService.CreateTokenAsync(user, _userManager)
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                return BadRequest(new { message = "Email address is already in use" });
            }

            var user = new AppUser
            {
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email,
                UserName = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded) return BadRequest(new { message = "Registration failed", errors = result.Errors });

            return Ok(new UserDto
            {
                Email = user.Email,
                DisplayName = user.DisplayName,
                Token = await _tokenService.CreateTokenAsync(user, _userManager)
            });
        }

        [Authorize]
        [HttpGet("current-user")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email!);

            return Ok(new UserDto
            {
                Email = user!.Email!,
                DisplayName = user.DisplayName,
                Token = await _tokenService.CreateTokenAsync(user, _userManager)
            });
        }
    }
}

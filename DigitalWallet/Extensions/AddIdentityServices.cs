using DigitalWallet.Application.Interfaces;
using DigitalWallet.Application.Services;
using DigitalWallet.Core.Entities.Identity;
using DigitalWallet.Core.Settings;
using System.Text;
using DigitalWallet.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DigitalWallet.Application.Extensions
{
    public static class IdentityServiceExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Bind JWT Settings using Options Pattern
            services.Configure<JwtSettings>(configuration.GetSection("JWT"));

            // 2. Register Identity Core
            services.AddIdentityCore<AppUser>(opt =>
            {
                opt.Password.RequireNonAlphanumeric = true;
                opt.Password.RequireDigit = true;
                opt.Password.RequireLowercase = true;
                opt.Password.RequireUppercase = true;
                opt.Password.RequiredLength = 6;
                opt.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager<SignInManager<AppUser>>();

            // 3. Register Token Service & Authentication
            services.AddScoped<ITokenService, TokenService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]!)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JWT:Audience"],
                    ValidateLifetime = true
                };
            });

            services.AddAuthorization();

            return services;
        }
    }
}

using DigitalWallet.API.Extensions;
using DigitalWallet.Application.Extensions;
using DigitalWallet.Application.Helper;
using DigitalWallet.Core.Entities.Identity;
using DigitalWallet.Core.Interfaces;
using DigitalWallet.Core.Settings;
using DigitalWallet.Infrastructure.Data;
using DigitalWallet.Infrastructure.Repositories;
using DigitalWallet.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfiles>());
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JWT"));

builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddCustomOpenApi();


builder.Services.AddControllers();
var app = builder.Build();

app.UseCustomScalar();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
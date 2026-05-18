using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EntityClassLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.API.Models.Auth;
using Server.Common.Exceptions;
using Server.Common.Helpers;
using Server.Core.Interfaces;
using Server.Infrastructure.Data;

namespace Server.Core.Services
{
    public class AuthService(ApplicationDbContext context, IConfiguration config, ILogger<AuthService> logger) 
        : IAuthService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IConfiguration _config = config;
        private readonly ILogger<AuthService> _logger = logger;

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            if (await _context.Users.AnyAsync(x => x.Login == request.Login, ct))
                throw new ValidationException("Пользователь с таким именем уже существует");

            User user = new()
            {
                Login = request.Login,
                Hash = PasswordHelper.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                YearOfBirth = request.YearOfBirth
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Пользователь {0} зарегистрирован", request.Login);

            return await GenerateAuthResponse(user);
        }


        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Login == request.Login, ct)
                ?? throw new UnauthorizedException("Неверный логин или пароль");

            if (!PasswordHelper.VerifyPassword(request.Password, user.Hash))
                throw new UnauthorizedException("Неверный логин или пароль");

            _logger.LogInformation("Пользователь {Login}", request.Login);
            return await GenerateAuthResponse(user);
        }
        public Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default)
        {
            // Не нужен
            return Task.FromResult(!string.IsNullOrEmpty(token));
        }
        private async Task<AuthResponse> GenerateAuthResponse(User user)
        {
            var token = GenerateJwtToken(user);
            return AuthResponse.Success(token, user);
        }
        private string GenerateJwtToken(User user)
        {
            var secret = _config[Common.Settings.JwtConstants.SecretKey]
                ?? throw new InvalidOperationException("JWT Secret отсутствует");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Login),
                new("firstName", user.FirstName)
            };

            claims.Add(new Claim(ClaimTypes.Role, "User"));

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(Common.Settings.JwtConstants.ExpiryHours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

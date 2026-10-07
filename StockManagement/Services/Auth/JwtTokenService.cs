using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StockManagement.DTOs.Auth;
using StockManagement.Entities;

namespace StockManagement.Services.Auth
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public LoginResponseDto GenerateToken(User user)
        {
            string issuer = GetRequiredSetting("Jwt:Issuer");
            string audience = GetRequiredSetting("Jwt:Audience");
            string key = GetRequiredSetting("Jwt:Key");

            int expirationMinutes =
                GetExpirationMinutes();

            byte[] keyBytes = GetKeyBytes(key);

            var securityKey =
                new SymmetricSecurityKey(keyBytes);

            var signingCredentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256);

            DateTime expiresAt =
                DateTime.UtcNow.AddMinutes(
                    expirationMinutes);

            var claims = new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.ToString()),

                new(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new(
                    ClaimTypes.Name,
                    user.Username),

                new(
                    JwtRegisteredClaimNames.Email,
                    user.Email),

                new(
                    ClaimTypes.Email,
                    user.Email),

                new(
                    ClaimTypes.Role,
                    user.Role.ToString()),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: signingCredentials);

            string accessToken =
                new JwtSecurityTokenHandler()
                    .WriteToken(token);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString()
            };
        }

        private string GetRequiredSetting(string key)
        {
            string? value = _configuration[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{key} yapılandırması bulunamadı.");
            }

            return value;
        }

        private int GetExpirationMinutes()
        {
            string expirationValue =
                GetRequiredSetting(
                    "Jwt:ExpirationMinutes");

            if (!int.TryParse(
                    expirationValue,
                    out int expirationMinutes) ||
                expirationMinutes <= 0 ||
                expirationMinutes > 1440)
            {
                throw new InvalidOperationException(
                    "Jwt:ExpirationMinutes değeri " +
                    "1 ile 1440 arasında olmalıdır.");
            }

            return expirationMinutes;
        }

        private static byte[] GetKeyBytes(string key)
        {
            try
            {
                byte[] keyBytes =
                    Convert.FromBase64String(key);

                if (keyBytes.Length < 32)
                {
                    throw new InvalidOperationException(
                        "JWT anahtarı en az 32 byte olmalıdır.");
                }

                return keyBytes;
            }
            catch (FormatException)
            {
                byte[] keyBytes =
                    Encoding.UTF8.GetBytes(key);

                if (keyBytes.Length < 32)
                {
                    throw new InvalidOperationException(
                        "JWT anahtarı en az 32 byte olmalıdır.");
                }

                return keyBytes;
            }
        }
    }
}
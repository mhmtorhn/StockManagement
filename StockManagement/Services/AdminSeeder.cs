using Microsoft.EntityFrameworkCore;
using StockManagement.Data;
using StockManagement.Entities;
using StockManagement.Enums;

namespace StockManagement.Services
{
    public class AdminSeeder
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminSeeder> _logger;

        public AdminSeeder(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AdminSeeder> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            bool userExists = await _context.Users.AnyAsync();

            if (userExists)
            {
                return;
            }

            string? username =
                _configuration["InitialAdmin:Username"];

            string? email =
                _configuration["InitialAdmin:Email"];

            string? password =
                _configuration["InitialAdmin:Password"];

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "İlk SuperAdmin bilgileri User Secrets " +
                    "içerisinde bulunamadı.");
            }

            ValidatePassword(password);

            var superAdmin = new User
            {
                Username = username.Trim().ToLowerInvariant(),
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        password,
                        workFactor: 12),
                Role = UserRole.SuperAdmin,
                IsActive = true,
                FailedLoginAttempts = 0,
                LockoutEnd = null,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = null
            };

            _context.Users.Add(superAdmin);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "İlk SuperAdmin hesabı oluşturuldu. Kullanıcı: {Username}",
                superAdmin.Username);
        }

        private static void ValidatePassword(string password)
        {
            if (password.Length < 12)
            {
                throw new InvalidOperationException(
                    "İlk SuperAdmin şifresi en az 12 karakter olmalıdır.");
            }

            bool hasUppercase = password.Any(char.IsUpper);
            bool hasLowercase = password.Any(char.IsLower);
            bool hasNumber = password.Any(char.IsDigit);
            bool hasSpecialCharacter =
                password.Any(character =>
                    !char.IsLetterOrDigit(character));

            if (!hasUppercase ||
                !hasLowercase ||
                !hasNumber ||
                !hasSpecialCharacter)
            {
                throw new InvalidOperationException(
                    "İlk SuperAdmin şifresi büyük harf, küçük harf, " +
                    "rakam ve özel karakter içermelidir.");
            }
        }
    }
}
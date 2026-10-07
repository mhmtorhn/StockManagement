using StockManagement.DTOs.Auth;
using StockManagement.Entities;

namespace StockManagement.Services.Auth
{
    public interface IJwtTokenService
    {
        LoginResponseDto GenerateToken(User user);
    }
}
using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<UserDto> RegisterAsync(RegisterDto registerDto);
    string GenerateJwtToken(UserDto user);
}
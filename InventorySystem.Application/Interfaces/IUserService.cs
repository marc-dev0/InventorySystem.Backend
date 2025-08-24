using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto?> GetByUsernameAsync(string username);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<IEnumerable<UserDto>> GetActiveUsersAsync();
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task UpdateAsync(int id, UpdateUserDto dto);
    Task DeleteAsync(int id);
    Task ChangePasswordAsync(int id, ChangePasswordDto dto);
    Task<bool> ValidatePasswordAsync(string username, string password);
    Task UpdateLastLoginAsync(int id);
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.API.Controllers.Base;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
public class UsersController : BaseCrudController<UserDto, CreateUserDto, UpdateUserDto>
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    // Implementation of abstract methods from BaseCrudController
    protected override async Task<IEnumerable<UserDto>> GetAllItemsAsync()
    {
        return await _userService.GetAllAsync();
    }

    protected override async Task<UserDto?> GetItemByIdAsync(int id)
    {
        return await _userService.GetByIdAsync(id);
    }

    protected override async Task<IEnumerable<UserDto>> GetActiveItemsAsync()
    {
        return await _userService.GetActiveUsersAsync();
    }

    [Authorize(Policy = "AdminOnly")]
    protected override async Task<UserDto> CreateItemAsync(CreateUserDto createDto)
    {
        return await _userService.CreateAsync(createDto);
    }

    protected override async Task UpdateItemAsync(int id, UpdateUserDto updateDto)
    {
        await _userService.UpdateAsync(id, updateDto);
    }

    [Authorize(Policy = "AdminOnly")]
    protected override async Task DeleteItemAsync(int id)
    {
        await _userService.DeleteAsync(id);
    }

    protected override int GetItemId(UserDto item)
    {
        return item.Id;
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    [HttpGet("username/{username}")]
    public async Task<ActionResult<UserDto>> GetByUsername(string username)
    {
        try
        {
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by username: {Username}", username);
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    [HttpGet("email/{email}")]
    public async Task<ActionResult<UserDto>> GetByEmail(string email)
    {
        try
        {
            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPut("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Only allow users to change their own password or admins to change any password
            var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value ?? "0");
            var currentUserRole = User.FindFirst("role")?.Value;

            if (currentUserId != id && currentUserRole != "Admin")
            {
                return Forbid("You can only change your own password");
            }

            await _userService.ChangePasswordAsync(id, dto);
            _logger.LogInformation("Password changed for user ID: {UserId}", id);
            
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Password change failed for user ID {UserId}: {Error}", id, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user ID: {UserId}", id);
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized();

            var user = await _userService.GetByIdAsync(currentUserId);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }
}
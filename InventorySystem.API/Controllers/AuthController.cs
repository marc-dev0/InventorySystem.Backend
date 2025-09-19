using Microsoft.AspNetCore.Mvc;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// User login
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Datos de entrada inválidos", errors = ModelState });

            var result = await _authService.LoginAsync(loginDto);
            if (result == null)
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", loginDto.Username);
                return Unauthorized(new { message = "Nombre de usuario o contraseña incorrectos" });
            }

            _logger.LogInformation("Successful login for username: {Username}", loginDto.Username);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", loginDto.Username);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// User registration
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _authService.RegisterAsync(registerDto);
            _logger.LogInformation("New user registered: {Username}", user.Username);
            
            return CreatedAtAction(
                nameof(UsersController.GetById), 
                "Users", 
                new { id = user.Id }, 
                user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed for username {Username}: {Error}", registerDto.Username, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username: {Username}", registerDto.Username);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Validate token (for frontend to check if token is still valid)
    /// </summary>
    [HttpGet("validate")]
    public IActionResult ValidateToken()
    {
        try
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Ok(new
                {
                    isValid = true,
                    username = User.Identity.Name,
                    role = User.FindFirst("role")?.Value
                });
            }

            return Unauthorized(new { isValid = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, "Error interno del servidor");
        }
    }
}
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;

namespace InventorySystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public AuthService(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        // Validate user credentials
        if (!await _userService.ValidatePasswordAsync(loginDto.Username, loginDto.Password))
        {
            return null;
        }

        // Get user details
        var user = await _userService.GetByUsernameAsync(loginDto.Username);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        // Update last login
        await _userService.UpdateLastLoginAsync(user.Id);

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var expirationMinutes = _configuration.GetValue<int>("JwtSettings:ExpirationInMinutes");

        return new AuthResponseDto
        {
            Token = token,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };
    }

    public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
    {
        var createUserDto = new CreateUserDto
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            Password = registerDto.Password,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Role = registerDto.Role,
            IsActive = true
        };

        return await _userService.CreateAsync(createUserDto);
    }

    public string GenerateJwtToken(UserDto user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expirationMinutes = jwtSettings.GetValue<int>("ExpirationInMinutes");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
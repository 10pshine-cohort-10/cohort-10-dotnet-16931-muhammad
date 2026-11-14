using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;
using TaskManagement.API.DTOs;
using TaskManagement.API.Entities;
using TaskManagement.API.Helpers;
using TaskManagement.API.Interfaces;

namespace TaskManagement.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists)
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists.", dto.Email);
            throw new InvalidOperationException("Email already registered.");
        }

        // Force regular registrations to always be "User" role
        var userRole = await _db.Roles.FirstAsync(r => r.Name == "User");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            RoleId = userRole.Id
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        user.Role = userRole;
        _logger.LogInformation("New user registered successfully: {Email}", user.Email);

        var (token, expires) = _tokenService.CreateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            Role = userRole.Name,
            ExpiresAt = expires
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // Ensure the user exists and include their Role
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user is null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", dto.Email);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        var (token, expires) = _tokenService.CreateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.Name,
            ExpiresAt = expires
        };
    }

    public Task LogoutAsync(int userId)
    {
        _logger.LogInformation("User with ID {UserId} logged out.", userId);

        return Task.CompletedTask;
    }
}
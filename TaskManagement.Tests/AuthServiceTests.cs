using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.API.Data;
using TaskManagement.API.DTOs;
using TaskManagement.API.Entities;
using TaskManagement.API.Interfaces;
using TaskManagement.API.Services;
using Xunit;

namespace TaskManagement.Tests;

public class AuthServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // 1. Setup In-Memory Database
        // We use a unique GUID so that every test gets a fresh, clean database in memory
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        // 2. Setup Mocks for Token Service and Logger
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        // Fake the token generation so it doesn't actually create a real JWT during testing
        _mockTokenService.Setup(t => t.CreateToken(It.IsAny<User>()))
            .Returns(("fake-jwt-token-for-testing", DateTime.UtcNow.AddHours(1)));

        // 3. Initialize the AuthService with our fakes
        _authService = new AuthService(_dbContext, _mockTokenService.Object, _mockLogger.Object);

        // 4. Seed the In-Memory DB with the default Role (Required for registration)
        _dbContext.Roles.Add(new Role { Id = 1, Name = "User" });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailIsUnique()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("fake-jwt-token-for-testing", result.Token);
        Assert.Equal(dto.Email, result.Email);

        // Verify the user was actually saved to the (in-memory) database
        var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        Assert.NotNull(userInDb);
        Assert.Equal("Test User", userInDb.FullName);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowException_WhenEmailAlreadyExists()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FullName = "Test User",
            Email = "duplicate@example.com",
            Password = "Password123!"
        };

        // Add the user to the fake database first
        await _authService.RegisterAsync(dto);

        // Act & Assert
        // Attempting to register the same email again should throw an InvalidOperationException
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.RegisterAsync(dto));

        Assert.Equal("Email already registered.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenPasswordIsWrong()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Login Test",
            Email = "login@test.com",
            Password = "CorrectPassword"
        };
        await _authService.RegisterAsync(registerDto);

        var loginDto = new LoginDto { Email = "login@test.com", Password = "WrongPassword" };

        // Act & Assert
        // A wrong password should throw an UnauthorizedAccessException
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(loginDto));
    }

    [Fact]
    public async Task LogoutAsync_ShouldCompleteSuccessfully_AndLogEvent()
    {
        // Arrange — register a user so we have a real UserId
        var registerDto = new RegisterDto
        {
            FullName = "Logout Test",
            Email = "logout@test.com",
            Password = "Password123!"
        };

        await _authService.RegisterAsync(registerDto);

        var userInDb = await _dbContext.Users
            .FirstAsync(u => u.Email == registerDto.Email);

        // Act
        await _authService.LogoutAsync(userInDb.Id);

        // Assert — verify the logout was logged with the correct user ID
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("logged out", StringComparison.OrdinalIgnoreCase)
                    && v.ToString()!.Contains(userInDb.Id.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
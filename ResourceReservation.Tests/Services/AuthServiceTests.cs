using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Services;
using ResourceReservation.Api.Security;
using ResourceReservation.Api.Services.Interfaces;
using FluentAssertions;

namespace ResourceReservation.Tests.Services;
public class AuthServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_ReturnsTokenDto_WhenCredentialsValid()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"Auth_Success_{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);

        var plainPassword = "P@ssw0rd!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword(plainPassword),
            Role = UserRole.User
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var tokenValue = "issued-token";
        var tokenServiceMock = new Mock<ITokenService>();
        tokenServiceMock.Setup(s => s.CreateToken(It.Is<User>(u => u.Id == user.Id)))
                        .Returns(tokenValue);

        var logger = NullLogger<AuthService>.Instance;
        var svc = new AuthService(db, tokenServiceMock.Object, logger);

        var dto = new LoginDto(user.Email, plainPassword);
        var result = await svc.AuthenticateAsync(dto);

        result.Should().NotBeNull();
        result!.Token.Should().Be(tokenValue);
        tokenServiceMock.Verify(s => s.CreateToken(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNull_WhenUserNotFound()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"Auth_NotFound_{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);

        var tokenServiceMock = new Mock<ITokenService>();
        var logger = NullLogger<AuthService>.Instance;
        var svc = new AuthService(db, tokenServiceMock.Object, logger);

        var dto = new LoginDto("nosuch@example.com", "irrelevant");
        var result = await svc.AuthenticateAsync(dto);

        result.Should().BeNull();
        tokenServiceMock.Verify(s => s.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNull_WhenPasswordInvalid()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"Auth_BadPassword_{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "test2@example.com",
            PasswordHash = PasswordHasher.HashPassword("CorrectPassword"),
            Role = UserRole.User
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var tokenServiceMock = new Mock<ITokenService>();
        var logger = NullLogger<AuthService>.Instance;
        var svc = new AuthService(db, tokenServiceMock.Object, logger);

        var dto = new LoginDto(user.Email, "WrongPassword");
        var result = await svc.AuthenticateAsync(dto);

        result.Should().BeNull();
        tokenServiceMock.Verify(s => s.CreateToken(It.IsAny<User>()), Times.Never);
    }
}
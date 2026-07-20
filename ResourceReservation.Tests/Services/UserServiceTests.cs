using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Services;
using ResourceReservation.Api.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceReservation.Tests.Services;
public class UserServiceTests
{
    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SearchAsync_NoFilters_ReturnsAllUsers()
    {
        using var db = CreateDb($"users_all_{Guid.NewGuid()}");
        var u1 = new User 
        { 
            Id = Guid.NewGuid(), 
            FirstName = "Alice", 
            LastName = "A", 
            Email = "a@example.com", 
            PasswordHash = PasswordHasher.HashPassword("pwd1"), 
            CreatedAt = DateTime.UtcNow.AddMinutes(-10) 
        };

        var u2 = new User 
        { 
            Id = Guid.NewGuid(), 
            FirstName = "Bob", 
            LastName = "B", 
            Email = "b@example.com", 
            PasswordHash = PasswordHasher.HashPassword("pwd2"), 
            CreatedAt = DateTime.UtcNow 
        };

        db.Users.AddRange(u1, u2);
        await db.SaveChangesAsync();

        var svc = new UserService(db, NullLogger<UserService>.Instance);
        var results = await svc.SearchAsync();

        results.Should().HaveCount(2);
        results.Select(r => r.Email).Should().Contain(new[] { "a@example.com", "b@example.com" });
    }

    [Fact]
    public async Task SearchAsync_InvalidRole_ReturnsEmpty()
    {
        using var db = CreateDb($"users_role_invalid_{Guid.NewGuid()}");
        var svc = new UserService(db, NullLogger<UserService>.Instance);

        var results = await svc.SearchAsync(role: "NotARole");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        using var db = CreateDb($"users_get_{Guid.NewGuid()}");
        var user = new User { Id = Guid.NewGuid(), FirstName = "Sam", LastName = "S", Email = "sam@example.com", PasswordHash = PasswordHasher.HashPassword("pw") };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = new UserService(db, NullLogger<UserService>.Instance);
        var dto = await svc.GetByIdAsync(user.Id);

        dto.Should().NotBeNull();
        dto!.Email.Should().Be("sam@example.com");
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenEmailExists()
    {
        using var db = CreateDb($"users_create_conflict_{Guid.NewGuid()}");
        var existing = new User 
        { 
            Id = Guid.NewGuid(), 
            FirstName = "Ex", 
            LastName = "Ist", 
            Email = "exi@example.com", 
            PasswordHash = PasswordHasher.HashPassword("x") 
        };

        db.Users.Add(existing);
        await db.SaveChangesAsync();

        var svc = new UserService(db, NullLogger<UserService>.Instance);
        var createDto = new UserCreateDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "exi@example.com",
            Password = "newpassword"
        };

        var result = await svc.CreateAsync(createDto);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesUser_WhenEmailNew()
    {
        using var db = CreateDb($"users_create_{Guid.NewGuid()}");
        var svc = new UserService(db, NullLogger<UserService>.Instance);
        var createDto = new UserCreateDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "new@example.com",
            Password = "securepassword"
        };

        var created = await svc.CreateAsync(createDto);

        created.Should().NotBeNull();
        created!.Email.Should().Be("new@example.com");

        var persisted = await db.Users.FirstOrDefaultAsync(u => u.Email == "new@example.com");
        persisted.Should().NotBeNull();
        persisted!.PasswordHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenNotFound()
    {
        using var db = CreateDb($"users_update_nf_{Guid.NewGuid()}");
        var svc = new UserService(db, NullLogger<UserService>.Instance);

        var ok = await svc.UpdateAsync(Guid.NewGuid(), new UserUpdateDto { FirstName = "X" });
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingUser()
    {
        using var db = CreateDb($"users_update_{Guid.NewGuid()}");
        var user = new User 
        { 
            Id = Guid.NewGuid(),
            FirstName = "Before", 
            LastName = "Name", 
            Email = "u1@example.com", 
            PasswordHash = PasswordHasher.HashPassword("p") 
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = new UserService(db, NullLogger<UserService>.Instance);
        var dto = new UserUpdateDto { FirstName = "After", Password = "newpass" };

        var ok = await svc.UpdateAsync(user.Id, dto);
        ok.Should().BeTrue();

        var persisted = await db.Users.FindAsync(user.Id);
        persisted.Should().NotBeNull();
        persisted!.FirstName.Should().Be("After");
        persisted.PasswordHash.Should().NotBe("newpass");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        using var db = CreateDb($"users_delete_nf_{Guid.NewGuid()}");
        var svc = new UserService(db, NullLogger<UserService>.Instance);

        var ok = await svc.DeleteAsync(Guid.NewGuid());
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser_WhenFound()
    {
        using var db = CreateDb($"users_delete_{Guid.NewGuid()}");
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            FirstName = "To", 
            LastName = "Delete", 
            Email = "del@example.com", 
            PasswordHash = PasswordHasher.HashPassword("p") 
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = new UserService(db, NullLogger<UserService>.Instance);
        var ok = await svc.DeleteAsync(user.Id);

        ok.Should().BeTrue();
        var persisted = await db.Users.FindAsync(user.Id);
        persisted.Should().BeNull();
    }
}


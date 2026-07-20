using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ResourceReservation.Tests.Services;
public class CategoryServiceTests
{
    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        using var db = CreateDb($"cat_all_{Guid.NewGuid()}");
        db.Categories.AddRange(
            new Category { Name = "A" },
            new Category { Name = "B" }
        );
        await db.SaveChangesAsync();

        var svc = new CategoryService(db, NullLogger<CategoryService>.Instance);
        var results = await svc.GetAllAsync();

        results.Should().HaveCount(2);
        results.Select(c => c.Name).Should().Contain(new[] { "A", "B" });
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCategory_WhenExists()
    {
        using var db = CreateDb($"cat_get_{Guid.NewGuid()}");
        var cat = new Category { Name = "TestCat" };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var svc = new CategoryService(db, NullLogger<CategoryService>.Instance);
        var dto = await svc.GetByIdAsync(cat.Id);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(cat.Id);
        dto.Name.Should().Be("TestCat");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateDb($"cat_get_nf_{Guid.NewGuid()}");
        var svc = new CategoryService(db, NullLogger<CategoryService>.Instance);

        var dto = await svc.GetByIdAsync(9999);
        dto.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsCategory()
    {
        using var db = CreateDb($"cat_create_{Guid.NewGuid()}");
        var svc = new CategoryService(db, NullLogger<CategoryService>.Instance);

        var createDto = new CategoryCreateDto { Name = "NewCategory" };
        var created = await svc.CreateAsync(createDto);

        created.Should().NotBeNull();
        created!.Name.Should().Be("NewCategory");
        created.Id.Should().BeGreaterThan(0);

        var persisted = await db.Categories.FindAsync(created.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("NewCategory");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenNotFound()
    {
        using var db = CreateDb($"cat_update_nf_{Guid.NewGuid()}");
        var svc = new CategoryService(db, NullLogger<CategoryService>.Instance);

        var ok = await svc.UpdateAsync(9999, new CategoryUpdateDto { Name = "X" });
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingCategory()
    {
        using var db = CreateDb($"cat_update_{Guid.NewGuid()}");
        var cat = new Category { Name = "Before" };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var svc = new CategoryService(db, NullLogger<CategoryService>.Instance);
        var dto = new CategoryUpdateDto { Name = "After" };

        var ok = await svc.UpdateAsync(cat.Id, dto);
        ok.Should().BeTrue();

        var persisted = await db.Categories.FindAsync(cat.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("After");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        using var db = CreateDb($"cat_delete_nf_{Guid.NewGuid()}");
        var svc = new CategoryService(db, NullLogger<CategoryService>.Instance);

        var ok = await svc.DeleteAsync(9999);
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesCategory_WhenFound()
    {
        using var db = CreateDb($"cat_delete_{Guid.NewGuid()}");
        var cat = new Category { Name = "ToDelete" };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var svc = new CategoryService(db, NullLogger<CategoryService>.Instance);
        var ok = await svc.DeleteAsync(cat.Id);

        ok.Should().BeTrue();
        var persisted = await db.Categories.FindAsync(cat.Id);
        persisted.Should().BeNull();
    }
}
    


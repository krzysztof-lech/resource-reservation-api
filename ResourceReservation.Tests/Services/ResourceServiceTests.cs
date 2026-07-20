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

namespace ResourceReservation.Tests.Services;
public class ResourceServiceTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SearchAsync_NoFilters_ReturnsAllResources()
    {
        using var db = CreateDb($"rs_all_{Guid.NewGuid()}");
        var r1 = new Resource { Id = Guid.NewGuid(), Name = "Room A" };
        var r2 = new Resource { Id = Guid.NewGuid(), Name = "Projector" };
        db.Resources.AddRange(r1, r2);
        await db.SaveChangesAsync();

        var svc = new ResourceService(db, NullLogger<ResourceService>.Instance);
        var results = await svc.SearchAsync();

        results.Should().HaveCount(2);
        results.Select(r => r.Name).Should().Contain(new[] { "Room A", "Projector" });
    }

    [Fact]
    public async Task SearchAsync_FiltersByQuery_Category_IsAvailable_Day_AndTime()
    {
        using var db = CreateDb($"rs_filters_{Guid.NewGuid()}");

        var monday = DayOfWeek.Monday;
        var resourceMatching = new Resource
        {
            Id = Guid.NewGuid(),
            Name = "Conference Room",
            Description = "Large room",
            IsAvailable = true,
            AllowedDaysRaw = "Monday,Tuesday",
            AvailableFrom = new TimeOnly(8, 0),
            AvailableTo = new TimeOnly(18, 0),
            CategoryId = 5
        };

        var resourceNoMatchDay = new Resource
        {
            Id = Guid.NewGuid(),
            Name = "Conference Room 2",
            Description = "Another",
            IsAvailable = true,
            AllowedDaysRaw = "Wednesday",
            AvailableFrom = new TimeOnly(8, 0),
            AvailableTo = new TimeOnly(18, 0),
            CategoryId = 5
        };

        var resourceNotAvailable = new Resource
        {
            Id = Guid.NewGuid(),
            Name = "Conference Room 3",
            Description = "Large room",
            IsAvailable = false,
            AllowedDaysRaw = "Monday",
            AvailableFrom = new TimeOnly(8, 0),
            AvailableTo = new TimeOnly(18, 0),
            CategoryId = 5
        };

        db.Resources.AddRange(resourceMatching, resourceNoMatchDay, resourceNotAvailable);
        await db.SaveChangesAsync();

        var svc = new ResourceService(db, NullLogger<ResourceService>.Instance);

        var results = await svc.SearchAsync(q: "Conference", categoryId: 5, isAvailable: true, day: monday, atTime: new TimeOnly(9, 0));
        results.Should().HaveCount(1);
        results.First().Name.Should().Be("Conference Room");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsResource_WhenExists()
    {
        using var db = CreateDb($"rs_get_{Guid.NewGuid()}");
        var resource = new Resource { Id = Guid.NewGuid(), Name = "Single" };
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        var svc = new ResourceService(db, NullLogger<ResourceService>.Instance);
        var dto = await svc.GetByIdAsync(resource.Id);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(resource.Id);
        dto.Name.Should().Be("Single");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateDb($"rs_get_notfound_{Guid.NewGuid()}");
        var svc = new ResourceService(db, NullLogger<ResourceService>.Instance);

        var dto = await svc.GetByIdAsync(Guid.NewGuid());
        dto.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsResource()
    {
        using var db = CreateDb($"rs_create_{Guid.NewGuid()}");
        var svc = new ResourceService(db, NullLogger<ResourceService>.Instance);

        var createDto = new ResourceCreateDto
        {
            Name = "New Room",
            Description = "desc",
            IsAvailable = true,
            SlotDurationMinutes = 30,
            AvailableFrom = new TimeOnly(9, 0),
            AvailableTo = new TimeOnly(17, 0),
            AllowedDays = new List<DayOfWeek> { DayOfWeek.Monday },
            CategoryId = 1
        };

        var created = await svc.CreateAsync(createDto);

        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
        created.Name.Should().Be("New Room");

        var persisted = await db.Resources.FindAsync(created.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("New Room");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenNotFound()
    {
        using var db = CreateDb($"rs_update_nf_{Guid.NewGuid()}");
        var svc = new ResourceService(db, NullLogger<ResourceService>.Instance);

        var ok = await svc.UpdateAsync(Guid.NewGuid(), new ResourceUpdateDto { Name = "X" });
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingResource()
    {
        using var db = CreateDb($"rs_update_{Guid.NewGuid()}");
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Name = "Old",
            AllowedDaysRaw = "Monday"
        };
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        var svc = new ResourceService(db, NullLogger<ResourceService>.Instance);
        var dto = new ResourceUpdateDto
        {
            Name = "Updated",
            AllowedDays = new List<DayOfWeek> { DayOfWeek.Friday }
        };

        var ok = await svc.UpdateAsync(resource.Id, dto);
        ok.Should().BeTrue();

        var persisted = await db.Resources.FindAsync(resource.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("Updated");
        persisted.AllowedDaysRaw.Should().Contain("Friday");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        using var db = CreateDb($"rs_delete_nf_{Guid.NewGuid()}");
        var svc = new ResourceService(db, NullLogger<ResourceService>.Instance);

        var ok = await svc.DeleteAsync(Guid.NewGuid());
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesResource_WhenFound()
    {
        using var db = CreateDb($"rs_delete_{Guid.NewGuid()}");
        var resource = new Resource { Id = Guid.NewGuid(), Name = "ToDelete" };
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        var svc = new ResourceService(db, NullLogger<ResourceService>.Instance);
        var ok = await svc.DeleteAsync(resource.Id);

        ok.Should().BeTrue();
        var persisted = await db.Resources.FindAsync(resource.Id);
        persisted.Should().BeNull();
    }
}
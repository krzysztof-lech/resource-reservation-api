using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Services;
using ResourceReservation.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceReservation.Tests.Services;
public class ReservationServiceTests
{
    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .Options;
        return new AppDbContext(options);
    }

    private static Resource NewResource(Guid id, bool isAvailable = true)
    {
        return new Resource
        {
            Id = id,
            Name = "Room",
            Description = "Room desc",
            IsAvailable = isAvailable,
            SlotDurationMinutes = 30,
            AvailableFrom = new TimeOnly(8, 0),
            AvailableTo = new TimeOnly(18, 0),
            AllowedDaysRaw = "Monday,Tuesday,Wednesday,Thursday,Friday"
        };
    }

    private static User NewUser(Guid id, string email)
    {
        return new User
        {
            Id = id,
            FirstName = "Fn",
            LastName = "Ln",
            Email = email,
            PasswordHash = "hash"
        };
    }

    private static DateTime NextWeekdayAtNineUtc()
    {
        var d = DateTime.UtcNow.Date.AddDays(1);
        while (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
            d = d.AddDays(1);
        return d.AddHours(9);
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenResourceNotFound()
    {
        using var db = CreateDb($"res_create_nf_{Guid.NewGuid()}");
        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);

        var dto = new CreateReservationDto(Guid.NewGuid(), DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2));
        var result = await svc.CreateAsync(dto, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenResourceNotAvailable()
    {
        using var db = CreateDb($"res_create_unavailable_{Guid.NewGuid()}");
        var resourceId = Guid.NewGuid();
        db.Resources.Add(NewResource(resourceId, isAvailable: false));
        await db.SaveChangesAsync();

        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);
        var dto = new CreateReservationDto(resourceId, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2));
        var result = await svc.CreateAsync(dto, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenDayNotAllowed()
    {
        using var db = CreateDb($"res_create_day_{Guid.NewGuid()}");
        var resourceId = Guid.NewGuid();
        var resource = NewResource(resourceId);
        resource.AllowedDaysRaw = "Wednesday";
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);

        var d = DateTime.UtcNow.Date.AddDays(1);
        while (d.DayOfWeek != DayOfWeek.Monday) d = d.AddDays(1);
        var dto = new CreateReservationDto(resourceId, d.AddHours(9), d.AddHours(10));
        var result = await svc.CreateAsync(dto, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenOutsideOperatingHours()
    {
        using var db = CreateDb($"res_create_hours_{Guid.NewGuid()}");
        var resourceId = Guid.NewGuid();
        var resource = NewResource(resourceId);
        resource.AvailableFrom = new TimeOnly(9, 0);
        resource.AvailableTo = new TimeOnly(17, 0);
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);
        var start = NextWeekdayAtNineUtc();
        var dto = new CreateReservationDto(resourceId, start, start.AddHours(1));
        var result = await svc.CreateAsync(dto, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenOverlappingReservationExists()
    {
        using var db = CreateDb($"res_create_overlap_{Guid.NewGuid()}");
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var resource = NewResource(resourceId);
        db.Resources.Add(resource);

        var baseStart = NextWeekdayAtNineUtc().AddHours(1);
        var existing = new Reservation
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            UserId = userId,
            StartTime = baseStart,
            EndTime = baseStart.AddHours(1),
            Status = new PendingStatus()
        };
        db.Reservations.Add(existing);
        await db.SaveChangesAsync();

        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);

        var dto = new CreateReservationDto(resourceId, baseStart.AddMinutes(30), baseStart.AddHours(1).AddMinutes(30));
        var result = await svc.CreateAsync(dto, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenParametersValid()
    {
        using var db = CreateDb($"res_create_success_{Guid.NewGuid()}");
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var resource = NewResource(resourceId);
        db.Resources.Add(resource);

        var user = NewUser(userId, "u@example.com");
        db.Users.Add(user);

        await db.SaveChangesAsync();

        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);

        var start = DateTime.UtcNow.AddHours(2);
        var end = start.AddHours(1);
        var dto = new CreateReservationDto(resourceId, start, end);

        var result = await svc.CreateAsync(dto, userId);

        result.Should().NotBeNull();
        var created = (result as ReservationPublicReadDto) ?? throw new Exception("Expected public DTO");
        created.ResourceId.Should().Be(resourceId);
        created.StartTime.Should().BeCloseTo(start, TimeSpan.FromSeconds(1));

        var persisted = await db.Reservations.FirstOrDefaultAsync(r => r.Id == created.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchAsync_FiltersByUserAndStatusAndPast()
    {
        using var db = CreateDb($"res_search_{Guid.NewGuid()}");
        var resourceId = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        db.Resources.Add(NewResource(resourceId));
        db.Users.Add(NewUser(user1, "a@example.com"));
        db.Users.Add(NewUser(user2, "b@example.com"));

        var now = DateTime.UtcNow;

        db.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            UserId = user1,
            StartTime = now.AddDays(-2),
            EndTime = now.AddDays(-1),
            Status = new PendingStatus()
        });

        var futureStart = NextWeekdayAtNineUtc().AddHours(2);
        db.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            UserId = user2,
            StartTime = futureStart,
            EndTime = futureStart.AddHours(1),
            Status = new ConfirmedStatus()
        });

        await db.SaveChangesAsync();

        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);

        var adminResults = await svc.SearchAsync(true, user2, "Confirmed", isPast: false);
        adminResults.Should().ContainSingle();
        adminResults.First().Should().BeOfType<ReservationReadDto>();

        var userResults = await svc.SearchAsync(false, user1, null, isPast: true);
        userResults.Should().ContainSingle();
        userResults.First().Should().BeOfType<ReservationPublicReadDto>();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateDb($"res_get_nf_{Guid.NewGuid()}");
        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);

        var dto = await svc.GetByIdAsync(Guid.NewGuid(), false);
        dto.Should().BeNull();
    }

    [Fact]
    public async Task CancelAsync_NotFound_ReturnsNotFound()
    {
        using var db = CreateDb($"res_cancel_nf_{Guid.NewGuid()}");
        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);

        var res = await svc.CancelAsync(Guid.NewGuid(), Guid.NewGuid(), false);
        res.Should().Be(CancelReservationResult.NotFound);
    }

    [Fact]
    public async Task CancelAsync_Forbidden_WhenRequesterNotOwner()
    {
        using var db = CreateDb($"res_cancel_forbidden_{Guid.NewGuid()}");
        var resourceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        db.Resources.Add(NewResource(resourceId));
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            UserId = ownerId,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Status = new PendingStatus()
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);
        var res = await svc.CancelAsync(reservation.Id, otherId, isAdmin: false);

        res.Should().Be(CancelReservationResult.Forbidden);
    }

    [Fact]
    public async Task CancelAsync_CannotCancel_WhenAlreadyCancelled()
    {
        using var db = CreateDb($"res_cancel_cannot_{Guid.NewGuid()}");
        var resourceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        db.Resources.Add(NewResource(resourceId));
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            UserId = ownerId,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Status = new CancelledStatus()
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);
        var res = await svc.CancelAsync(reservation.Id, ownerId, isAdmin: true);

        res.Should().Be(CancelReservationResult.CannotCancel);
    }

    [Fact]
    public async Task CancelAsync_Succeeds_ForOwner()
    {
        using var db = CreateDb($"res_cancel_ok_{Guid.NewGuid()}");
        var resourceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        db.Resources.Add(NewResource(resourceId));
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            UserId = ownerId,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Status = new PendingStatus()
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);
        var res = await svc.CancelAsync(reservation.Id, ownerId, isAdmin: false);

        res.Should().Be(CancelReservationResult.Success);

        var persisted = await db.Reservations.FindAsync(reservation.Id);
        persisted!.Status.DisplayName.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelAsync_Succeeds_ForAdmin()
    {
        using var db = CreateDb($"res_cancel_admin_{Guid.NewGuid()}");
        var resourceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        db.Resources.Add(NewResource(resourceId));
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            UserId = ownerId,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Status = new PendingStatus()
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var svc = new ReservationService(db, NullLogger<ReservationService>.Instance);
        var res = await svc.CancelAsync(reservation.Id, adminId, isAdmin: true);

        res.Should().Be(CancelReservationResult.Success);

        var persisted = await db.Reservations.FindAsync(reservation.Id);
        persisted!.Status.DisplayName.Should().Be("Cancelled");
    }
}


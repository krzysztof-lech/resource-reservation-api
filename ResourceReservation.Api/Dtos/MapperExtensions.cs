using ResourceReservation.Api.Models;
using ResourceReservation.Api.Security;

namespace ResourceReservation.Api.Dtos;

public static class MapperExtensions
{
    // Resource
    public static ResourceReadDto ToReadDto(this Resource r) =>
        new()
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsAvailable = r.IsAvailable,
            SlotDurationMinutes = r.SlotDurationMinutes,
            AvailableFrom = r.AvailableFrom,
            AvailableTo = r.AvailableTo,
            AllowedDays = r.AllowedDays ?? new(),
            CategoryId = r.CategoryId,
            CategoryName = r.Category?.Name
        };

    public static Resource ToEntity(this ResourceCreateDto dto) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            IsAvailable = dto.IsAvailable,
            SlotDurationMinutes = dto.SlotDurationMinutes,
            AvailableFrom = dto.AvailableFrom,
            AvailableTo = dto.AvailableTo,
            AllowedDays = dto.AllowedDays ?? new(),
            CategoryId = dto.CategoryId
        };

    public static void ApplyUpdates(this Resource existing, ResourceUpdateDto dto)
    {
        if (dto.Name is not null) existing.Name = dto.Name;
        if (dto.Description is not null) existing.Description = dto.Description;
        if (dto.IsAvailable.HasValue) existing.IsAvailable = dto.IsAvailable.Value;
        if (dto.SlotDurationMinutes.HasValue) existing.SlotDurationMinutes = dto.SlotDurationMinutes.Value;
        if (dto.AvailableFrom.HasValue) existing.AvailableFrom = dto.AvailableFrom.Value;
        if (dto.AvailableTo.HasValue) existing.AvailableTo = dto.AvailableTo.Value;
        if (dto.AllowedDays is not null) existing.AllowedDays = dto.AllowedDays;
        if (dto.CategoryId is not null) existing.CategoryId = dto.CategoryId;
    }

    // User
    public static UserReadDto ToReadDto(this User u) =>
        new()
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            Role = u.Role.ToString(),
            CreatedAt = u.CreatedAt
        };

    public static User ToEntity(this UserCreateDto dto, bool assignAdminRoleAllowed = false)
    {
        var u = new User
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = PasswordHasher.HashPassword(dto.Password)
        };

        return u;
    }

    public static void ApplyUpdates(this User existing, UserUpdateDto dto)
    {
        if (dto.FirstName is not null) existing.FirstName = dto.FirstName;
        if (dto.LastName is not null) existing.LastName = dto.LastName;
        if (dto.Email is not null) existing.Email = dto.Email;
        if (dto.Role is not null && Enum.TryParse<UserRole>(dto.Role, out var r)) existing.Role = r;
        if (dto.Password is not null) existing.PasswordHash = PasswordHasher.HashPassword(dto.Password);
    }

    // Category
    public static CategoryReadDto ToReadDto(this Category c) =>
        new()
        {
            Id = c.Id,
            Name = c.Name
        };

    public static Category ToEntity(this CategoryCreateDto dto) =>
        new()
        {
            Name = dto.Name
        };

    public static void ApplyUpdates(this Category existing, CategoryUpdateDto dto)
    {
        if (dto.Name is not null) existing.Name = dto.Name;
    }

    // Reservation
    public static ReservationReadDto ToReadDto(this Reservation r) =>
        new(
            Id: r.Id,
            ResourceId: r.ResourceId,
            ResourceName: r.Resource?.Name ?? "",
            UserId: r.UserId,
            UserEmail: r.User?.Email ?? "",
            StartTime: r.StartTime,
            EndTime: r.EndTime,
            Status: r.Status.DisplayName
        );

    public static ReservationPublicReadDto ToPublicReadDto(this Reservation r) =>
        new(
            Id: r.Id,
            ResourceId: r.ResourceId,
            ResourceName: r.Resource?.Name ?? "",
            StartTime: r.StartTime,
            EndTime: r.EndTime,
            Status: r.Status.DisplayName
        );

    public static Reservation ToEntity(this CreateReservationDto dto, Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ResourceId = dto.ResourceId,
            UserId = userId,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Status = new PendingStatus()
        };
}
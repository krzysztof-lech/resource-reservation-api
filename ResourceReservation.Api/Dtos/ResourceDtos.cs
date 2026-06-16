namespace ResourceReservation.Api.Dtos;

public record ResourceReadDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsAvailable { get; init; }
    public int SlotDurationMinutes { get; init; }
    public TimeOnly AvailableFrom { get; init; }
    public TimeOnly AvailableTo { get; init; }
    public List<DayOfWeek> AllowedDays { get; init; } = new();
    public int? CategoryId { get; init; }
    public string? CategoryName { get; init; }
}

public record ResourceCreateDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsAvailable { get; init; } = true;
    public int SlotDurationMinutes { get; init; } = 30;
    public TimeOnly AvailableFrom { get; init; } = new(8, 0);
    public TimeOnly AvailableTo { get; init; } = new(17, 0);
    public List<DayOfWeek>? AllowedDays { get; init; }
    public int? CategoryId { get; init; }
}

public record ResourceUpdateDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsAvailable { get; init; }
    public int? SlotDurationMinutes { get; init; }
    public TimeOnly? AvailableFrom { get; init; }
    public TimeOnly? AvailableTo { get; init; }
    public List<DayOfWeek>? AllowedDays { get; init; }
    public int? CategoryId { get; init; }
}

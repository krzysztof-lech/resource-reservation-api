namespace ResourceReservation.Api.Dtos;

public record CategoryReadDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
}

public record CategoryCreateDto
{
    public required string Name { get; init; }
}

public record CategoryUpdateDto
{
    public required string Name { get; init; }
}
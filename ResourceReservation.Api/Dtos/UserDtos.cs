namespace ResourceReservation.Api.Dtos;

public record UserReadDto
{
    public Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? Role { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UserCreateDto
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? Role { get; init; }
}

public record UserUpdateDto
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? Role { get; init; }
}

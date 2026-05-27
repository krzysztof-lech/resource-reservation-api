namespace ResourceReservation.Api.Models;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}

namespace ResourceReservation.Api.Models;
public class ResourceImage
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public Resource? Resource { get; set; }
    public required string FileName { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}


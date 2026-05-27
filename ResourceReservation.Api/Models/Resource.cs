using System.ComponentModel.DataAnnotations.Schema;

namespace ResourceReservation.Api.Models;
public class Resource
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int SlotDurationMinutes { get; set; } = 30;
    public TimeOnly AvailableFrom { get; set; } = new TimeOnly(8, 0);
    public TimeOnly AvailableTo { get; set; } = new TimeOnly(17, 0);
    public string AllowedDaysRaw { get; set; } = "Monday,Tuesday,Wednesday,Thursday,Friday";
    [NotMapped]
    public List<DayOfWeek> AllowedDays
    {
        get => AllowedDaysRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(Enum.Parse<DayOfWeek>)
                             .ToList();
        set => AllowedDaysRaw = string.Join(",", value);
    }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
}


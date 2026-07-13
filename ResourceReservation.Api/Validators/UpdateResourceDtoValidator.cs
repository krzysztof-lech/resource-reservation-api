using FluentValidation;
using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Validators;

public class UpdateResourceDtoValidator : AbstractValidator<ResourceUpdateDto>
{
    public UpdateResourceDtoValidator()
    {
        RuleFor(r => r.Name)
            .MinimumLength(2).WithMessage("Resource name must be at least 2 characters long.")
            .MaximumLength(100).WithMessage("Resource name cannot exceed 100 characters.")
            .When(r => r.Name is not null);

        RuleFor(r => r.SlotDurationMinutes)
            .GreaterThan(0).WithMessage("Slot duration must be greater than 0 minutes.")
            .When(r => r.SlotDurationMinutes.HasValue);

        RuleFor(r => r.AvailableTo)
            .GreaterThan(r => r.AvailableFrom)
            .WithMessage("Available To time must be after Available From time.")
            .When(r => r.AvailableTo.HasValue && r.AvailableFrom.HasValue);
    }
}
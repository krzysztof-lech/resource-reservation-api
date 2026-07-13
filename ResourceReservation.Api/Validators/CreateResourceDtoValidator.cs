using FluentValidation;
using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Validators;

public class CreateResourceDtoValidator : AbstractValidator<ResourceCreateDto>
{
    public CreateResourceDtoValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Resource name is required.")
            .MinimumLength(2).WithMessage("Resource name must be at least 2 characters long.")
            .MaximumLength(100).WithMessage("Resource name cannot exceed 100 characters.");

        RuleFor(r => r.SlotDurationMinutes)
            .GreaterThan(0).WithMessage("Slot duration must be greater than 0 minutes.");

        RuleFor(r => r.AvailableTo)
            .GreaterThan(r => r.AvailableFrom)
            .WithMessage("Available To time must be after Available From time.");
    }
}
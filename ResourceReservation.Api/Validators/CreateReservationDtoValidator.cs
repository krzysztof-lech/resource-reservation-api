using FluentValidation;
using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Validators;

public class CreateReservationDtoValidator : AbstractValidator<CreateReservationDto>
{
    public CreateReservationDtoValidator()
    {
        RuleFor(x => x.ResourceId)
            .NotEmpty()
            .WithMessage("Resource ID is required.");

        RuleFor(x => x.StartTime)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Reservation start time must be in the future.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Reservation end time must be after the start time.");
    }
}

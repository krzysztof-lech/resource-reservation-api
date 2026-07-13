using FluentValidation;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Models;

namespace ResourceReservation.Api.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UserUpdateDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(u => u.FirstName)
            .MinimumLength(2).WithMessage("First name must be at least 2 characters long.")
            .When(u => u.FirstName is not null);

        RuleFor(u => u.LastName)
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters long.")
            .When(u => u.LastName is not null);

        RuleFor(u => u.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .When(u => u.Email is not null);

        RuleFor(u => u.Password)
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .When(u => u.Password is not null);

        RuleFor(u => u.Role)
            .Must(role => Enum.TryParse<UserRole>(role, true, out _))
            .WithMessage("Invalid user role.")
            .When(u => u.Role is not null);
    }
}
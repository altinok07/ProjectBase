using FluentValidation;
using ProjectBase.Application.Commands.Users;
using ProjectBase.Application.Constants;
using System.Text.RegularExpressions;

namespace ProjectBase.Application.Validators.Users;

public class UserCreateCommandValidator : AbstractValidator<UserCreateCommand>
{
    public UserCreateCommandValidator()
    {
        RuleFor(user => user.Name)
            .NotEmpty().WithMessage(ValidationMessages.User.NameRequired)
            .MinimumLength(2).WithMessage(ValidationMessages.User.NameMinimumLength)
            .MaximumLength(256).WithMessage(ValidationMessages.User.NameMaximumLength);

        RuleFor(user => user.Surname)
            .NotEmpty().WithMessage(ValidationMessages.User.SurnameRequired)
            .MinimumLength(2).WithMessage(ValidationMessages.User.SurnameMinimumLength)
            .MaximumLength(256).WithMessage(ValidationMessages.User.SurnameMaximumLength);

        RuleFor(user => user.Email)
            .NotEmpty().WithMessage(ValidationMessages.User.EmailRequired)
            .MaximumLength(256).WithMessage(ValidationMessages.User.EmailMaximumLength)
            .EmailAddress().WithMessage(ValidationMessages.User.EmailInvalidFormat);

        RuleFor(user => user.Phone)
            .NotEmpty().WithMessage(ValidationMessages.User.PhoneRequired)
            .MinimumLength(10).WithMessage(ValidationMessages.User.PhoneMinimumLength)
            .MaximumLength(20).WithMessage(ValidationMessages.User.PhoneMaximumLength)
            .Matches(new Regex(@"^[\d\s\-\+\(\)]+$")).WithMessage(ValidationMessages.User.PhoneInvalidFormat);

        RuleFor(user => user.Password)
            .NotEmpty().WithMessage(ValidationMessages.User.PasswordRequired)
            .MinimumLength(8).WithMessage(ValidationMessages.User.PasswordMinimumLength)
            .MaximumLength(100).WithMessage(ValidationMessages.User.PasswordMaximumLength)
            .Matches("[A-Z]").WithMessage(ValidationMessages.User.PasswordMustContainUppercase)
            .Matches("[a-z]").WithMessage(ValidationMessages.User.PasswordMustContainLowercase)
            .Matches("[0-9]").WithMessage(ValidationMessages.User.PasswordMustContainDigit);
    }
}

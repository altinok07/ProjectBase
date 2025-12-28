using FluentValidation;
using ProjectBase.Application.Queries.Users;
using static ProjectBase.Application.Constants.ValidationMessages.UserValidations;

namespace ProjectBase.Application.Validators.Users;

public class UserLoginQueryValidator : AbstractValidator<UserLoginQuery>
{
    public UserLoginQueryValidator()
    {
        // Mail: Required, EmailAddress
        RuleFor(user => user.Mail)
            .NotEmpty().WithMessage(MailRequired)
            .EmailAddress().WithMessage(MailInvalidFormat);

        // Password: Required
        RuleFor(user => user.Password)
            .NotEmpty().WithMessage(PasswordRequired);
    }
}

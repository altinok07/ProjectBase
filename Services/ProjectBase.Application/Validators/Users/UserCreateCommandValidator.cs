using FluentValidation;
using ProjectBase.Application.Commands.Users;
using static ProjectBase.Application.Constants.ValidationMessages.UserCreateCommandValidations;



namespace ProjectBase.Application.Validators.Users;

/// <summary>
/// UserCreateCommand için validasyon kuralları
/// UserConfiguration'daki property kurallarına göre düzenlenmiştir
/// </summary>
public class UserCreateCommandValidator : AbstractValidator<UserCreateCommand>
{
    public UserCreateCommandValidator()
    {
        // Name: Required, MaxLength(64)
        RuleFor(user => user.Name)
            .NotEmpty().WithMessage(NameRequired)
            .MaximumLength(64).WithMessage(NameMaximumLength);

        // Surname: Required, MaxLength(64)
        RuleFor(user => user.Surname)
            .NotEmpty().WithMessage(SurnameRequired)
            .MaximumLength(64).WithMessage(SurnameMaximumLength);

        // Mail: Required, MaxLength(64), EmailAddress
        RuleFor(user => user.Mail)
            .NotEmpty().WithMessage(MailRequired)
            .MaximumLength(64).WithMessage(MailMaximumLength)
            .EmailAddress().WithMessage(MailInvalidFormat);

        // Password: Required, MinimumLength(8) - PasswordHash MaxLength(128) olduğu için hash sonrası uzunluk kontrolü yapılmaz
        RuleFor(user => user.Password)
            .NotEmpty().WithMessage(PasswordRequired)
            .MinimumLength(8).WithMessage(PasswordMinimumLength);
    }
}

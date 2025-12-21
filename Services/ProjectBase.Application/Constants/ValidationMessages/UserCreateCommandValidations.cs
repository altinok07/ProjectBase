namespace ProjectBase.Application.Constants.ValidationMessages;

public static class UserCreateCommandValidations
{
    // Name validasyon mesajları (MaxLength: 64, Required)
    public const string NameRequired = "Ad alanı boş olamaz";
    public const string NameMaximumLength = "Ad alanı en fazla 64 karakter olabilir";

    // Surname validasyon mesajları (MaxLength: 64, Required)
    public const string SurnameRequired = "Soyad alanı boş olamaz";
    public const string SurnameMaximumLength = "Soyad alanı en fazla 64 karakter olabilir";

    // Mail validasyon mesajları (MaxLength: 64, Required, EmailFormat)
    public const string MailRequired = "E-posta alanı boş olamaz";
    public const string MailMaximumLength = "E-posta alanı en fazla 64 karakter olabilir";
    public const string MailInvalidFormat = "E-posta adresi geçerli formatta değil";

    // Password validasyon mesajları (Required, MinLength: 8)
    public const string PasswordRequired = "Parola alanı boş olamaz";
    public const string PasswordMinimumLength = "Parola en az 8 karakter olmalıdır";
}

namespace ProjectBase.Application.Constants;

/// <summary>
/// Validation mesajları için constant değerler.
/// FluentValidation kullanımında mesajları merkezi olarak yönetmek için kullanılır.
/// </summary>
public static class ValidationMessages
{
    // Genel Mesajlar
    public static class Common
    {
        public const string Required = "{0} boş olamaz";
        public const string MinimumLength = "{0} en az {1} karakter olmalıdır";
        public const string MaximumLength = "{0} en fazla {1} karakter olabilir";
    }

    // Kullanıcı Validasyon Mesajları
    public static class User
    {
        public const string NameRequired = "Kullanıcı Ad boş olamaz";
        public const string NameMinimumLength = "Kullanıcı Ad en az {MinLength} karakter olmalıdır";
        public const string NameMaximumLength = "Kullanıcı Ad en fazla {MaxLength} karakter olabilir";

        public const string SurnameRequired = "Kullanıcı Soyad boş olamaz";
        public const string SurnameMinimumLength = "Kullanıcı Soyad en az {MinLength} karakter olmalıdır";
        public const string SurnameMaximumLength = "Kullanıcı Soyad en fazla {MaxLength} karakter olabilir";

        public const string EmailRequired = "E-mail boş olamaz";
        public const string EmailMaximumLength = "E-mail en fazla {MaxLength} karakter olabilir";
        public const string EmailInvalidFormat = "E-mail adresi uygun formatta değil";

        public const string PhoneRequired = "Telefon No boş olamaz";
        public const string PhoneMinimumLength = "Telefon No en az {MinLength} karakter olmalıdır";
        public const string PhoneMaximumLength = "Telefon No en fazla {MaxLength} karakter olabilir";
        public const string PhoneInvalidFormat = "Telefon No sadece rakam, boşluk, tire, artı ve parantez içerebilir";

        public const string PasswordRequired = "Parola boş olamaz";
        public const string PasswordMinimumLength = "Parola en az {MinLength} karakter olmalıdır";
        public const string PasswordMaximumLength = "Parola en fazla {MaxLength} karakter olabilir";
        public const string PasswordMustContainUppercase = "Parola en az bir büyük harf içermelidir";
        public const string PasswordMustContainLowercase = "Parola en az bir küçük harf içermelidir";
        public const string PasswordMustContainDigit = "Parola en az bir rakam içermelidir";
    }
}

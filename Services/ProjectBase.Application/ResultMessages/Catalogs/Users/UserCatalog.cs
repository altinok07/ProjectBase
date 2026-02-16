using System.Collections.ObjectModel;
using ProjectBase.Application.ResultMessages.Enums;

namespace ProjectBase.Application.ResultMessages.Catalogs.Users;
public static class UserCatalog
{
    public static IReadOnlyDictionary<Enum, string> TrUserCatalog { get; } =
        new ReadOnlyDictionary<Enum, string>(new Dictionary<Enum, string>
        {
            [UserMessages.UserAlreadyCreated] = "Bu Mail Adresi ile Sistemde Kayıtlı Kullanıcı Mevcut",
            [UserMessages.LoginFailed] = "Kullanıcı Adı Şifre Yanlış. Giriş İşlemi Başarısız",
            [UserMessages.LoginSuccess] = "Sisteme Giriş Başarılı",
            [UserMessages.UserNotFound] = "Kullanıcı Bulunamadı",
            [UserMessages.UserPasswordNotCorrect] = "Geçerli Şifrenizi Yanlış Girdiniz",
            [UserMessages.UserPasswordNotConfirmed] = "Yeni Şifre İle Şifre Tekrarı Eşleşmiyor"
        });

    public static IReadOnlyDictionary<Enum, string> EnUserCatalog { get; } =
        new ReadOnlyDictionary<Enum, string>(new Dictionary<Enum, string>
        {
            [UserMessages.UserAlreadyCreated] = "A user with this email address already exists",
            [UserMessages.LoginFailed] = "Incorrect username or password. Sign-in failed",
            [UserMessages.LoginSuccess] = "Login successful",
            [UserMessages.UserNotFound] = "User not found",
            [UserMessages.UserPasswordNotCorrect] = "Your current password is incorrect",
            [UserMessages.UserPasswordNotConfirmed] = "New password and confirmation do not match"
        });
}

using System.Collections.ObjectModel;
using ProjectBase.Application.ResultMessages.Enums;

namespace ProjectBase.Application.ResultMessages.Catalogs;

public static class CommonCatalog
{
    public static IReadOnlyDictionary<Enum, string> TrCommonCatalog { get; } =
        new ReadOnlyDictionary<Enum, string>(new Dictionary<Enum, string>
        {
            [CommonMessages.Success] = "İşlem Başarılı",
            [CommonMessages.Fail] = "İşlem Başarısız"
        });

    public static IReadOnlyDictionary<Enum, string> EnCommonCatalog { get; } =
        new ReadOnlyDictionary<Enum, string>(new Dictionary<Enum, string>
        {
            [CommonMessages.Success] = "Succeed",
            [CommonMessages.Fail] = "Failed"
        });
}
using ProjectBase.Application.ResultMessages.Catalogs;
using ProjectBase.Application.ResultMessages.Catalogs.Users;
using ProjectBase.Application.ResultMessages.Enums;
using ProjectBase.Core.Localization.Interfaces;

namespace ProjectBase.Application.ResultMessages;
public class TrCatalog : ICultureCatalog
{
    public string Culture => "tr-tr";

    public IReadOnlyDictionary<Type, IReadOnlyDictionary<Enum, string>> Map { get; } = new Dictionary<Type, IReadOnlyDictionary<Enum, string>>
    {
        [typeof(CommonMessages)] = CommonCatalog.TrCommonCatalog,
        [typeof(UserMessages)] = UserCatalog.TrUserCatalog
    };
}
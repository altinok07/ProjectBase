using AutoMapper;
using ProjectBase.Application.Commands.Users;
using ProjectBase.Domain.Entities.Users;
using ProjectBase.Domain.Enums;
using ProjectBase.Model.ResponseModels.Users;

namespace ProjectBase.Application.Mappings.Users;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserCreateCommand>()
            .ReverseMap()
            .ForMember(user => user.UserTypeId, map => map.MapFrom(request => (int)UserTypeEnum.TenantUser));

        CreateMap<User, UserResponse>().ReverseMap();
    }
}
using AutoMapper;
using ProjectBase.Application.Commands.Users;
using ProjectBase.Domain.Entities.Users;
using ProjectBase.Model.ResponseModels.Users;

namespace ProjectBase.Application.Mappings.Users;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserCreateCommand>().ReverseMap();
        CreateMap<User, UserResponse>().ReverseMap();
    }
}

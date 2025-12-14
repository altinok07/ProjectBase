using AutoMapper;
using ProjectBase.Application.Commands.Users;
using ProjectBase.Domain.Entities.Users;

namespace ProjectBase.Application.Mappings.Users;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserCreateCommand>().ReverseMap();
    }
}

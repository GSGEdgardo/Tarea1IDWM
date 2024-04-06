using AutoMapper;
using courses_dotnet_api.Src.Models;
using courses_dotnet_api.Src.DTOs.Account;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<User, PasswordDto>()
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.PasswordHash))
            .ForMember(dest => dest.PasswordSalt, opt => opt.MapFrom(src => src.PasswordSalt));
    }
}
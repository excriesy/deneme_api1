using AutoMapper;
using ShareVault.API.DTOs;
using ShareVault.API.Models;

namespace ShareVault.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserDto>();
            CreateMap<RegisterDto, User>();
            CreateMap<User, UserDetailsDto>();

            // File mappings
            CreateMap<FileModel, FileDto>();
            CreateMap<FileModel, FileDetailsDto>();
            CreateMap<SharedFile, SharedFileDto>();
        }
    }
} 
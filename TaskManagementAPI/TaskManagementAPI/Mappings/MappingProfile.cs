using AutoMapper;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Mappings
{
    public class MappingProfile: Profile 
    {
        public MappingProfile() 
        {
            #region User Mappings
            // Map from UserRequest DTO to entity
            CreateMap<UserRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())// PasswordHash will be set separately
                .ForMember(dest => dest.Role, opt => opt.Ignore()).ReverseMap();

            // Map from entity to UserResponse DTO
            CreateMap<User, UserResponse>();
            #endregion

            #region Task Mappings
            // Map from TaskRequest DTO to entity
            CreateMap<TaskRequest, TaskObject>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Prevent overwriting ID during update
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.PriorityLevel))
                .ForMember(dest => dest.User, opt => opt.Ignore()) ;// handled via FK (UserId)

            // Map from entity to TaskResponse DTO
            CreateMap<TaskObject, TaskResponse>()
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.Owner, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreatedDate));
            #endregion
        }
    }
}

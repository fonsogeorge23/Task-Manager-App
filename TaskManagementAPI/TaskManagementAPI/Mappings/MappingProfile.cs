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
            // Map from request DTO to entity
            CreateMap<TaskRequest, TaskObject>()
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.PriorityLevel))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(_ => DateTime.Now));

            // Map from entity to response DTO
            CreateMap<TaskObject, TaskResponse>()
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.Owner, opt => opt.MapFrom(src => src.User.Username));
        }
    }
}

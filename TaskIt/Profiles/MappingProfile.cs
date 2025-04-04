using AutoMapper;
using TaskIt.DTOs;
using TaskIt.Models;

namespace TaskIt.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Task mappings
            CreateMap<TaskItem, TaskDTO>()
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => 
                    src.CreatedBy != null ? $"{src.CreatedBy.FirstName} {src.CreatedBy.LastName}" : "Unknown"))
                .ForMember(dest => dest.AssignedToName, opt => opt.MapFrom(src => 
                    src.AssignedTo != null ? $"{src.AssignedTo.FirstName} {src.AssignedTo.LastName}" : null));
            
            CreateMap<CreateTaskDTO, TaskItem>();
            
            CreateMap<UpdateTaskDTO, TaskItem>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // Notification mappings
            CreateMap<Notification, NotificationDTO>()
                .ForMember(dest => dest.TaskTitle, opt => opt.MapFrom(src => 
                    src.Task != null ? src.Task.Title : null));
            
            CreateMap<CreateNotificationDTO, Notification>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(_ => false));

            // User mappings
            CreateMap<ApplicationUser, UserDTO>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
        }
    }

    public class UserDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public DateTime LastActive { get; set; }
    }
}

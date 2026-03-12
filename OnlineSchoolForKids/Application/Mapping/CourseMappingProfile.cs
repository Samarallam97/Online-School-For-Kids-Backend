using AutoMapper;
using Domain.Entities.Content;


namespace Application.Mapping
{
    public class CourseMappingProfile : Profile
    {// Course -> CourseDto
        public CourseMappingProfile()
        {
            CreateMap<Course, CourseDto>()

               .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src =>
                   src.Category != null ? src.Category.Name : "Unknown"))
               .ForMember(dest => dest.LevelDisplay, opt => opt.MapFrom(src => src.AgeGroup.ToString()))
               .ForMember(dest => dest.IsInWishlist, opt => opt.Ignore()) // Set manually
               .ForMember(dest => dest.IsInCart, opt => opt.Ignore()); // Set manually

        }

    }
}


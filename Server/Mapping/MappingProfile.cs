using System.Runtime.CompilerServices;
using AutoMapper;
using SkillSnap.Server.Models;
using SkillSnap.Shared;

namespace SkillSnap.Server.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Project, ProjectDto>()
            .ForCtorParam("Tags", opt => opt.MapFrom(src => (src.TagsCsv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));


        CreateMap<ProjectCreateDto, Project>()
            .ForMember(d => d.TagsCsv, opt => opt.MapFrom(s => string.Join(",", s.Tags ?? Array.Empty<string>())));
    }
}
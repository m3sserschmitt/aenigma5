using AutoMapper;
using Enigma5.App.Data;
using Enigma5.App.Models;

namespace Enigma5.App;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<AdjacencyList, Neighborhood>();
        CreateMap<BroadcastAdjacencyList, Vertex>()
        .ForMember(dest => dest.Neighborhood, opt => opt.MapFrom(src => src.GetAdjacencyList()));
        CreateMap<Vertex, BroadcastAdjacencyList>();
        CreateMap<Neighborhood, AdjacencyList>();
        CreateMap<Data.PendingMessage, Models.PendingMessage>();
    }
}

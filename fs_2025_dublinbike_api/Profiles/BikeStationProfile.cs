using AutoMapper;
using fs_2025_dublinbike_api.DTOs;
using fs_2025_dublinbike_api.Models;

namespace fs_2025_dublinbike_api.Profiles
{
    public class BikeStationProfile : Profile
    {
        public BikeStationProfile()
        {
            // Map BikeStation to BikeStationDTO
            CreateMap<BikeStation, BikeStationDto>()
                .ForMember(dest => dest.Lat, opt =>
                    opt.MapFrom(src => src.position != null ? src.position.lat : 0))
                .ForMember(dest => dest.Lng, opt =>
                    opt.MapFrom(src => src.position != null ? src.position.lng : 0))
                .ForMember(dest => dest.LastUpdateUtc, opt =>
                    opt.MapFrom(src => DateTimeOffset.FromUnixTimeMilliseconds(src.last_update)))
                .ForMember(dest => dest.LastUpdateLocal, opt =>
                    opt.MapFrom(src => DateTimeOffset.FromUnixTimeMilliseconds(src.last_update))) // TEMP: same as UTC
                .ForMember(dest => dest.Occupancy, opt =>
                    opt.MapFrom(src => src.bike_stands > 0
                        ? Math.Round((double)src.available_bikes / src.bike_stands, 3)
                        : 0));
        }
    }

}


using AutoMapper;
using fs_2025_dublinbike_API_.Models;
using fs_2025_dublinbike_API_.DTOs;
using System;

/// <summary>
/// AutoMapper profile that defines mappings between BikeStation domain models
/// and BikeStationDto data transfer objects.
/// </summary>

namespace fs_2025_dublinbike_API_.Profiles
{
    public class BikeStationProfile : Profile
    {
        // Configure mapping from BikeStation (Model) → BikeStationDto (DTO)
        public BikeStationProfile()
        {
            CreateMap<BikeStation, BikeStationDto>()
                .ForMember(d => d.id, o => o.MapFrom(s => s.id))
                .ForMember(d => d.Number, o => o.MapFrom(s => s.number))
                .ForMember(d => d.ContractName, o => o.MapFrom(s => s.contract_name))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.name))
                .ForMember(d => d.Address, o => o.MapFrom(s => s.address))
                .ForMember(d => d.Lat, o => o.MapFrom(s => s.position!.lat))
                .ForMember(d => d.Lng, o => o.MapFrom(s => s.position!.lng))
                .ForMember(d => d.BikeStands, o => o.MapFrom(s => s.bike_stands))
                .ForMember(d => d.AvailableBikeStands, o => o.MapFrom(s => s.available_bike_stands))
                .ForMember(d => d.AvailableBikes, o => o.MapFrom(s => s.available_bikes))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.status))
                .ForMember(d => d.LastUpdateUtc, o => o.MapFrom(s => s.last_update_utc))
                .ForMember(d => d.LastUpdateLocal, o => o.MapFrom(s => s.last_update_local))
                .ForMember(d => d.Occupancy,
                    o => o.MapFrom(s =>
                        s.bike_stands == 0 ? 0 :
                        (double)s.available_bikes / s.bike_stands
                    ));
        }
    }
}


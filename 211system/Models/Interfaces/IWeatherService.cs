using _211system.Models.Dtos;

namespace _211system.Models.Interfaces
{
    public interface IWeatherService
    {
        Task<GroundWeatherDto> GetGroundConditionsAsync(double lat, double lng);
        Task<FlightWeatherDto> GetFlightConditionsAsync(double lat, double lng);
    }
}
using _211system.Models.Dtos.Nasa;

namespace _211system.Models.Interfaces
{
    public interface INasaService
    {
        Task<NasaFetchResultDto> FetchFireDataAndCreateIncidentsAsync();
    }
}

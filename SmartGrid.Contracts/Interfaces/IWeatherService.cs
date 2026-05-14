using CoreWCF;
using SmartGrid.Contracts.DTOs;

namespace SmartGrid.Contracts.Interfaces
{
    [ServiceContract]
    public interface IWeatherService
    {
        [OperationContract]
        WeatherDataDto GetCurrentWeather(string city);

        [OperationContract]

        List<WeatherDataDto> GetWeatherForAllNodes();
    }
}
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using Contracts.DTOs;

namespace Contracts.Interfaces
{
    [ServiceContract]
    public interface IWeatherService
    {
        [OperationContract]
        [WebGet(UriTemplate = "/current/{city}",
                ResponseFormat = WebMessageFormat.Json)]
        WeatherDataDto GetCurrentWeather(string city);

        [OperationContract]
        [WebGet(UriTemplate = "/all",
                ResponseFormat = WebMessageFormat.Json)]
        List<WeatherDataDto> GetWeatherForAllNodes();

        [OperationContract]
        [WebGet(UriTemplate = "/correlation",
                ResponseFormat = WebMessageFormat.Json)]
        WeatherCorrelationDto GetWeatherCorrelation();
    }
}
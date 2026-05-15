using System.ServiceModel;
using System.ServiceModel.Web;
using Contracts.DTOs;

namespace Contracts.Interfaces
{
    [ServiceContract]
    public interface ICarbonService
    {
        [OperationContract]
        [WebGet(UriTemplate = "/intensity/{zone}",
                ResponseFormat = WebMessageFormat.Json)]
        CarbonIntensityDto GetCarbonIntensity(string zone);
    }
}
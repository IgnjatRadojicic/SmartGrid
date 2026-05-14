using CoreWCF;
using SmartGrid.Contracts.DTOs;

namespace SmartGrid.Contracts.Interfaces
{
    [ServiceContract]
    public interface ICarbonService
    {
        [OperationContract]
        CarbonIntensityDto GetCarbonIntensity(string zone);
    }
}
using System.Runtime.Serialization;

namespace SmartGrid.Contracts.DTOs
{
    [DataContract]
    public class CarbonIntensityDto
    {
        [DataMember] public string Zone { get; set; } = string.Empty;         // "RS"
        [DataMember] public double CarbonIntensity { get; set; } // gCO2eq/kWh
        [DataMember] public double FossilFuelPercentage { get; set; }
        [DataMember] public double RenewablePercentage { get; set; }
        [DataMember] public Dictionary<string, double> PowerBreakdown { get; set; } = new Dictionary<string, double>(); // "coal": 45.2, "hydro": 30.1, etc.
        [DataMember] public DateTime FetchedAt { get; set; }
    }
}
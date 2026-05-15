using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class CarbonIntensityDto
    {
        [DataMember] public string Zone { get; set; }
        [DataMember] public double CarbonIntensity { get; set; }
        [DataMember] public double FossilFuelPercentage { get; set; }
        [DataMember] public double RenewablePercentage { get; set; }
        [DataMember] public Dictionary<string, double> PowerBreakdown { get; set; }
        [DataMember] public string FetchedAt { get; set; }
    }
}
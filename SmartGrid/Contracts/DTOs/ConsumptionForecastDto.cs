using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class ConsumptionForecastDto
    {
        [DataMember] public string NodeId { get; set; }
        [DataMember] public List<ForecastPointDto> ForecastPoints { get; set; }
        [DataMember] public string GeneratedAt { get; set; }
    }

    [DataContract]
    public class ForecastPointDto
    {
        [DataMember] public string Timestamp { get; set; }
        [DataMember] public double PredictedPower { get; set; }
        [DataMember] public double Confidence { get; set; }
    }
}
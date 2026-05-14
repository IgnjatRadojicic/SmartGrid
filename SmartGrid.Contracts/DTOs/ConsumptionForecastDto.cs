using System.Runtime.Serialization;

namespace SmartGrid.Contracts.DTOs
{
    [DataContract]
    public class ConsumptionForecastDto
    {
        [DataMember] public string NodeId { get; set; } = string.Empty;
        [DataMember] public List<ForecastPointDto> ForecastPoints { get; set; } = new List<ForecastPointDto>();
        [DataMember] public DateTime GeneratedAt { get; set; }
    }

    [DataContract]
    public class ForecastPointDto
    {
        [DataMember] public DateTime Timestamp { get; set; }
        [DataMember] public double PredictedPower { get; set; }
        [DataMember] public double Confidence { get; set; }      // 0.0 - 1.0
    }
}
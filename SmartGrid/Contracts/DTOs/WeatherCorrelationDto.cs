using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class WeatherCorrelationDto
    {
        [DataMember] public string GeneratedAt { get; set; }
        [DataMember] public List<NodeWeatherCorrelationDto> NodeCorrelations { get; set; }
    }

    [DataContract]
    public class NodeWeatherCorrelationDto
    {
        [DataMember] public string City { get; set; }
        [DataMember] public double Temperature { get; set; }
        [DataMember] public double Humidity { get; set; }
        [DataMember] public double WindSpeed { get; set; }
        [DataMember] public string WeatherDescription { get; set; }
        [DataMember] public double RiskFactor { get; set; }
        [DataMember] public double AvgPowerUsage { get; set; }
        [DataMember] public double EstimatedPowerUsage { get; set; }
        [DataMember] public string RiskLevel { get; set; }
    }
}
using System.Runtime.Serialization;

namespace SmartGrid.Contracts.DTOs
{
    [DataContract]
    public class WeatherDataDto
    {
        [DataMember] public string City { get; set; } = string.Empty;
        [DataMember] public double Temperature { get; set; }     // Celsius
        [DataMember] public double Humidity { get; set; }        // %
        [DataMember] public double WindSpeed { get; set; }       // m/s
        [DataMember] public string Description { get; set; } = string.Empty;     // "clear sky", "rain", etc.
        [DataMember] public double Pressure { get; set; }        // hPa
        [DataMember] public DateTime FetchedAt { get; set; }
    }
}
using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class WeatherDataDto
    {
        [DataMember] public string City { get; set; }
        [DataMember] public double Temperature { get; set; }
        [DataMember] public double Humidity { get; set; }
        [DataMember] public double WindSpeed { get; set; }
        [DataMember] public string Description { get; set; }
        [DataMember] public double Pressure { get; set; }
        [DataMember] public string FetchedAt { get; set; }
    }
}
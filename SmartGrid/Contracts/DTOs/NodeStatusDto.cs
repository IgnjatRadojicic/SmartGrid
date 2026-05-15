using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class NodeStatusDto
    {
        [DataMember] public string NodeId { get; set; }
        [DataMember] public string NodeName { get; set; }
        [DataMember] public string City { get; set; }
        [DataMember] public double Latitude { get; set; }
        [DataMember] public double Longitude { get; set; }
        [DataMember] public string Role { get; set; }
        [DataMember] public double Voltage { get; set; }
        [DataMember] public double Current { get; set; }
        [DataMember] public double Power { get; set; }
        [DataMember] public double Frequency { get; set; }
        [DataMember] public int FaultIndicator { get; set; }
        [DataMember] public string Status { get; set; }            // "Normal", "Warning", "Critical"
        [DataMember] public string LastUpdated { get; set; }
    }
}
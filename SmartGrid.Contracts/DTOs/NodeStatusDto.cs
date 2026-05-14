using System.Runtime.Serialization;

namespace SmartGrid.Contracts.DTOs
{
    [DataContract]
    public class NodeStatusDto
    {
        [DataMember] public string NodeId { get; set; } = string.Empty;
        [DataMember] public string NodeName { get; set; } = string.Empty;
        [DataMember] public string City { get; set; } = string.Empty;
        [DataMember] public double Latitude { get; set; }
        [DataMember] public double Longitude { get; set; }
        [DataMember] public string Role { get; set; } = string.Empty;          // "Supplier" / "Consumer"
        [DataMember] public double CurrentPower { get; set; }
        [DataMember] public double CurrentTau { get; set; }
        [DataMember] public double CurrentGamma { get; set; }
        [DataMember] public string Status { get; set; } = string.Empty;         // "Normal", "Warning", "Critical"
        [DataMember] public bool IsStable { get; set; }
        [DataMember] public DateTime LastUpdated { get; set; }
    }
}
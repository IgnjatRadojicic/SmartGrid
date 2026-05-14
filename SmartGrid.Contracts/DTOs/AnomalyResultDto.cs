using System.Runtime.Serialization;

namespace SmartGrid.Contracts.DTOs
{
    [DataContract]
    public class AnomalyResultDto
    {
        [DataMember] public int ReadingId { get; set; }
        [DataMember] public DateTime DetectedAt { get; set; }
        [DataMember] public string AnomalyType { get; set; } = string.Empty;  // "HighInstability", "PowerImbalance", "SlowReaction"
        [DataMember] public string Severity { get; set; } = string.Empty;       // "Low", "Medium", "High", "Critical"
        [DataMember] public double StabValue { get; set; }
        [DataMember] public double ZScore { get; set; }
        [DataMember] public string AffectedNode { get; set; } = string.Empty;   // "Node1", "Node2", etc.
        [DataMember] public string Description { get; set; } = string.Empty;
    }
}
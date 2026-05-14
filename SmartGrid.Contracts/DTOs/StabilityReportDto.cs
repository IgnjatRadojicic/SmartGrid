using System.Runtime.Serialization;

namespace SmartGrid.Contracts.DTOs
{
    [DataContract]
    public class StabilityReportDto
    {
        [DataMember] public int TotalReadings { get; set; }
        [DataMember] public int StableCount { get; set; }
        [DataMember] public int UnstableCount { get; set; }
        [DataMember] public double StabilityPercentage { get; set; }
        [DataMember] public double AvgStabValue { get; set; }
        [DataMember] public double MaxStabValue { get; set; }
        [DataMember] public double MinStabValue { get; set; }
        [DataMember] public DateTime GeneratedAt { get; set; }
        [DataMember] public List<NodeSummaryDto> NodeSummaries { get; set; } = new List<NodeSummaryDto>();
    }

    [DataContract]
    public class NodeSummaryDto
    {
        [DataMember] public string NodeName { get; set; } = string.Empty;        // "Obrenovac", "Beograd", "Novi Sad", "Nis"
        [DataMember] public string NodeId { get; set; } = string.Empty;       // "Node1" - "Node4"
        [DataMember] public double AvgPower { get; set; }
        [DataMember] public double AvgReactionTime { get; set; }
        [DataMember] public double AvgElasticity { get; set; }
    }
}
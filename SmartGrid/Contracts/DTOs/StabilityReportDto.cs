using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class StabilityReportDto
    {
        [DataMember] public int TotalReadings { get; set; }
        [DataMember] public int FaultCount { get; set; }
        [DataMember] public int NormalCount { get; set; }
        [DataMember] public int WarningCount { get; set; }
        [DataMember] public double AvgVoltage { get; set; }
        [DataMember] public double AvgCurrent { get; set; }
        [DataMember] public double AvgPower { get; set; }
        [DataMember] public double AvgFrequency { get; set; }
        [DataMember] public double MaxPower { get; set; }
        [DataMember] public double MinFrequency { get; set; }
        [DataMember] public double MaxFrequency { get; set; }
        [DataMember] public int FrequencyAnomalyCount { get; set; }
        [DataMember] public int PowerOverloadCount { get; set; }
        [DataMember] public string GeneratedAt { get; set; }
        [DataMember] public List<NodeSummaryDto> NodeSummaries { get; set; }
    }

    [DataContract]
    public class NodeSummaryDto
    {
        [DataMember] public string NodeId { get; set; }
        [DataMember] public string NodeName { get; set; }
        [DataMember] public string City { get; set; }
        [DataMember] public double AvgPower { get; set; }
        [DataMember] public double AvgFrequency { get; set; }
        [DataMember] public int FaultCount { get; set; }
    }
}
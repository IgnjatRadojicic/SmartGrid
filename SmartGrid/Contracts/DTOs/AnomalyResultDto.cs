using System;
using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class AnomalyResultDto
    {
        [DataMember] public int ReadingId { get; set; }
        [DataMember] public string DetectedAt { get; set; }
        [DataMember] public string AnomalyType { get; set; }       // "FrequencySpike", "PowerOverload", "Fault"
        [DataMember] public string Severity { get; set; }           // "Low", "Medium", "High", "Critical"
        [DataMember] public double Value { get; set; }              // deltaF ili P(t)
        [DataMember] public double Threshold { get; set; }          // prag koji je probijen
        [DataMember] public string AffectedNode { get; set; }
        [DataMember] public string Description { get; set; }
    }
}
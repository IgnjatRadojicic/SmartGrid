using System.Runtime.Serialization;

namespace SmartGrid.Contracts.DTOs
{
    [DataContract]
    public class NotificationDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public DateTime Timestamp { get; set; }
        [DataMember] public string Type { get; set; } = string.Empty;           // "Anomaly", "Threshold", "Fault", "Info"
        [DataMember] public string Severity { get; set; } = string.Empty;       // "Low", "Medium", "High", "Critical"
        [DataMember] public string Message { get; set; } = string.Empty;
        [DataMember] public string NodeId { get; set; } = string.Empty;
        [DataMember] public bool IsRead { get; set; }
    }
}
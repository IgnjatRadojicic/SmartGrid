using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class NotificationDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public string Timestamp { get; set; }
        [DataMember] public string Type { get; set; }              // "FrequencyAnomaly", "PowerOverload", "Fault", "Info"
        [DataMember] public string Severity { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public string NodeId { get; set; }
        [DataMember] public bool IsRead { get; set; }
    }
}
using System;
using System.Runtime.Serialization;

namespace Contracts.DTOs
{
    [DataContract]
    public class GridReadingDto
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public DateTime Timestamp { get; set; }
        [DataMember] public double Voltage { get; set; }
        [DataMember] public double Current { get; set; }
        [DataMember] public double PowerUsage { get; set; }
        [DataMember] public double Frequency { get; set; }
        [DataMember] public int FaultIndicator { get; set; }
        [DataMember] public double[] FFT { get; set; }

        // Computed - P(t) = V(t) * I(t) po specifikaciji
        [DataMember] public double CalculatedPower { get; set; }

        // Formatiran timestamp za frontend (WCF DateTime fix)
        [DataMember] public string TimestampFormatted { get; set; }
    }
}
using System.Runtime.Serialization;

namespace SmartGrid.Contracts.DTOs
{
    [DataContract]
    public class GridReadingDto
    {
        [DataMember] public int Id { get; set; }

        // Reaction times (sekunde)
        [DataMember] public double Tau1 { get; set; }
        [DataMember] public double Tau2 { get; set; }
        [DataMember] public double Tau3 { get; set; }
        [DataMember] public double Tau4 { get; set; }

        // Nominal power (proizvodnja/potrosnja)
        [DataMember] public double P1 { get; set; }
        [DataMember] public double P2 { get; set; }
        [DataMember] public double P3 { get; set; }
        [DataMember] public double P4 { get; set; }

        // Price elasticity coefficients (gamma)
        [DataMember] public double G1 { get; set; }
        [DataMember] public double G2 { get; set; }
        [DataMember] public double G3 { get; set; }
        [DataMember] public double G4 { get; set; }

        // Stabilnost
        [DataMember] public double Stab { get; set; }
        [DataMember] public string StabF { get; set; } = string.Empty;

        // Sinteticki timestamp 
        [DataMember] public DateTime Timestamp { get; set; }
    }
}
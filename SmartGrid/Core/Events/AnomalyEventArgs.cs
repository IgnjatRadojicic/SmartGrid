using System;
using Contracts.DTOs;

namespace Core.Events
{
    public class AnomalyEventArgs : EventArgs
    {
        public AnomalyResultDto Anomaly { get; private set; }
        public GridReadingDto Reading { get; private set; }

        public AnomalyEventArgs(AnomalyResultDto anomaly, GridReadingDto reading)
        {
            Anomaly = anomaly;
            Reading = reading;
        }
    }
}
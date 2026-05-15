using System;

namespace Core.Events
{
    public class ThresholdEventArgs : EventArgs
    {
        public string NodeId { get; private set; }
        public string MetricName { get; private set; }
        public double Value { get; private set; }
        public double Threshold { get; private set; }
        public DateTime OccurredAt { get; private set; }

        public ThresholdEventArgs(string nodeId, string metricName, double value, double threshold)
        {
            NodeId = nodeId;
            MetricName = metricName;
            Value = value;
            Threshold = threshold;
            OccurredAt = DateTime.UtcNow;
        }
    }
}
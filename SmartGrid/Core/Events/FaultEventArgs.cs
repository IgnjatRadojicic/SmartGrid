using System;

namespace Core.Events
{
    public class FaultEventArgs : EventArgs
    {
        public string NodeId { get; private set; }
        public string NodeName { get; private set; }
        public string FaultType { get; private set; }
        public string Description { get; private set; }
        public DateTime OccurredAt { get; private set; }

        public FaultEventArgs(string nodeId, string nodeName, string faultType, string description)
        {
            NodeId = nodeId;
            NodeName = nodeName;
            FaultType = faultType;
            Description = description;
            OccurredAt = DateTime.UtcNow;
        }
    }
}
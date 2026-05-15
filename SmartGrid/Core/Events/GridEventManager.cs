using System;
using System.Collections.Generic;
using Contracts.DTOs;

namespace Core.Events
{
    // Centralni event hub Svi eventi prolaze ovde.
    // Cuva notifikacije u memoriji za frontend polling
    public class GridEventManager
    {
        // Custom delegati
        public delegate void AnomalyDetectedHandler(object sender, AnomalyEventArgs e);
        public delegate void ThresholdExceededHandler(object sender, ThresholdEventArgs e);
        public delegate void FaultDetectedHandler(object sender, FaultEventArgs e);

        // Eventi
        public event AnomalyDetectedHandler AnomalyDetected;
        public event ThresholdExceededHandler ThresholdExceeded;
        public event FaultDetectedHandler FaultDetected;

        private readonly List<NotificationDto> _notifications = new List<NotificationDto>();
        private int _notificationId = 0;
        private readonly object _lock = new object();

        public GridEventManager()
        {
            // Self-subscribe: svaki event kreira notifikaciju
            AnomalyDetected += OnAnomalyDetected;
            ThresholdExceeded += OnThresholdExceeded;
            FaultDetected += OnFaultDetected;
        }

        public void RaiseAnomalyDetected(AnomalyEventArgs e)
        {
            if (AnomalyDetected != null)
                AnomalyDetected(this, e);
        }

        public void RaiseThresholdExceeded(ThresholdEventArgs e)
        {
            if (ThresholdExceeded != null)
                ThresholdExceeded(this, e);
        }

        public void RaiseFaultDetected(FaultEventArgs e)
        {
            if (FaultDetected != null)
                FaultDetected(this, e);
        }

        private void OnAnomalyDetected(object sender, AnomalyEventArgs e)
        {
            AddNotification(new NotificationDto
            {
                Type = e.Anomaly.AnomalyType,
                Severity = e.Anomaly.Severity,
                Message = e.Anomaly.Description,
                NodeId = e.Anomaly.AffectedNode,
            });
        }

        private void OnThresholdExceeded(object sender, ThresholdEventArgs e)
        {
            AddNotification(new NotificationDto
            {
                Type = "Threshold",
                Severity = "Medium",
                Message = string.Format("{0} na {1} = {2:F2} (prag: {3:F2})",
                    e.MetricName, e.NodeId, e.Value, e.Threshold),
                NodeId = e.NodeId,
            });
        }

        private void OnFaultDetected(object sender, FaultEventArgs e)
        {
            AddNotification(new NotificationDto
            {
                Type = "Fault",
                Severity = "High",
                Message = string.Format("Kvar na {0}: {1}", e.NodeName, e.Description),
                NodeId = e.NodeId,
            });
        }

        private void AddNotification(NotificationDto notification)
        {
            lock (_lock)
            {
                _notificationId++;
                notification.Id = _notificationId;
                notification.Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
                notification.IsRead = false;
                _notifications.Add(notification);

                if (_notifications.Count > 500)
                    _notifications.RemoveAt(0);
            }
        }

        public List<NotificationDto> GetRecentNotifications(int count = 50)
        {
            lock (_lock)
            {
                int skip = Math.Max(0, _notifications.Count - count);
                return _notifications.GetRange(skip, _notifications.Count - skip);
            }
        }

        public List<NotificationDto> GetUnreadNotifications()
        {
            lock (_lock)
            {
                return _notifications.FindAll(n => !n.IsRead);
            }
        }

        public void MarkAllAsRead()
        {
            lock (_lock)
            {
                _notifications.ForEach(n => n.IsRead = true);
            }
        }
    }
}
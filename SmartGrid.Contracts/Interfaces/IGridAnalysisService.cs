using CoreWCF;
using SmartGrid.Contracts.DTOs;

namespace SmartGrid.Contracts.Interfaces
{
    [ServiceContract]
    public interface IGridAnalysisService
    {
        [OperationContract]
        List<GridReadingDto> GetReadingDtos(int page, int pageSize);

        [OperationContract]
        GridReadingDto GetReadingById(int id);

        [OperationContract]
        StabilityReportDto GetStabilityReport();

        [OperationContract]
        List<AnomalyResultDto> DetectAnomalies(double zScoreThreshold);

        [OperationContract]
        List<NodeStatusDto> GetAllNodeStatuses();

        [OperationContract]
        NodeStatusDto GetNodeStatus(string nodeId);

        [OperationContract]
        ConsumptionForecastDto GetConsumptionForecast(string nodeId, int points);

        [OperationContract]
        List<NotificationDto> GetRecentNotifications(int count);

        [OperationContract]
        byte[] ExportCompressedData();


    }
}
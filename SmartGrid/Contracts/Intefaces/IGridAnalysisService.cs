using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using Contracts.DTOs;

namespace Contracts.Interfaces
{
    [ServiceContract]
    public interface IGridAnalysisService
    {
        [OperationContract]
        [WebGet(UriTemplate = "/readings?page={page}&pageSize={pageSize}",
                ResponseFormat = WebMessageFormat.Json)]
        List<GridReadingDto> GetReadings(int page, int pageSize);

        [OperationContract]
        [WebGet(UriTemplate = "/readings/{id}",
                ResponseFormat = WebMessageFormat.Json)]
        GridReadingDto GetReadingById(string id);

        [OperationContract]
        [WebGet(UriTemplate = "/report",
                ResponseFormat = WebMessageFormat.Json)]
        StabilityReportDto GetStabilityReport();

        [OperationContract]
        [WebGet(UriTemplate = "/anomalies?freqThreshold={freqThreshold}&powerThreshold={powerThreshold}",
                ResponseFormat = WebMessageFormat.Json)]
        List<AnomalyResultDto> DetectAnomalies(string freqThreshold, string powerThreshold);

        [OperationContract]
        [WebGet(UriTemplate = "/nodes",
                ResponseFormat = WebMessageFormat.Json)]
        List<NodeStatusDto> GetAllNodeStatuses();

        [OperationContract]
        [WebGet(UriTemplate = "/nodes/{nodeId}",
                ResponseFormat = WebMessageFormat.Json)]
        NodeStatusDto GetNodeStatus(string nodeId);

        [OperationContract]
        [WebGet(UriTemplate = "/forecast/{nodeId}?points={points}",
                ResponseFormat = WebMessageFormat.Json)]
        ConsumptionForecastDto GetConsumptionForecast(string nodeId, string points);

        [OperationContract]
        [WebGet(UriTemplate = "/notifications?count={count}",
                ResponseFormat = WebMessageFormat.Json)]
        List<NotificationDto> GetRecentNotifications(string count);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/export",
                   ResponseFormat = WebMessageFormat.Json)]
        byte[] ExportCompressedData();
    }
}
using System.Runtime.Versioning;
using ServicePanel.Models;

namespace ServicePanel;

[SupportedOSPlatform("windows")]
public interface IServiceControl : IGrainWithStringKey
{
    Task<List<ServiceModel>> GetAllServices();

    Task<ServiceModel> GetService(string serviceName);

    Task<string> StartService(string serviceName);

    Task<string> StopService(string serviceName);

    Task<string> RestartService(string serviceName);

    Task<ServiceSummaryModel> GetServiceSummaries();
}
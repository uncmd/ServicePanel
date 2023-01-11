using System.Runtime.Versioning;
using ServicePanel.Models;

namespace ServicePanel.Grains;

[SupportedOSPlatform("windows")]
public interface IServiceControlGrain : IGrainWithStringKey
{
    Task<List<ServiceModel>> GetAllServices();

    Task<ServiceModel> GetService(string serviceName);

    Task<string> StartService(string serviceName);

    Task<string> StopService(string serviceName);

    Task<string> RestartService(string serviceName);

    Task<ServiceSummaryModel> GetServiceSummaries();
}
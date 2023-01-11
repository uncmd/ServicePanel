using ServicePanel.Grains;
using ServicePanel.Models;
using System.Runtime.Versioning;

namespace ServicePanel.Admin.Services;

[SupportedOSPlatform("windows")]
public class ServiceControlService
{
    private readonly ISiloDetailsProvider siloDetailsProvider;
    private readonly IClusterClient clusterClient;

    public ServiceControlService(
        ISiloDetailsProvider siloDetailsProvider,
        IClusterClient clusterClient)
    {
        this.siloDetailsProvider = siloDetailsProvider;
        this.clusterClient = clusterClient;
    }

    public async Task<ServiceModel[]> GetAllServices()
    {
        List<ServiceModel> services = new List<ServiceModel>();

        var allSilos = await siloDetailsProvider.GetSiloDetails();

        foreach (var silloAddress in allSilos.Select(p => p.EndpointAddress))
        {
            var siloServices = await clusterClient.GetGrain<IServiceControlGrain>(silloAddress).GetAllServices();

            services.AddRange(siloServices);
        }

        return services.ToArray();
    }

    public async Task<ServiceModel[]> GetAllServices(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return Array.Empty<ServiceModel>();

        return (await clusterClient.GetGrain<IServiceControlGrain>(address).GetAllServices()).ToArray();
    }

    public async Task<ServiceSummaryModel[]> GetServiceSummaries()
    {
        List<ServiceSummaryModel> services = new List<ServiceSummaryModel>();

        var allSilos = await siloDetailsProvider.GetSiloDetails();

        foreach (var silloAddress in allSilos.Select(p => p.EndpointAddress))
        {
            var siloSummary = await clusterClient.GetGrain<IServiceControlGrain>(silloAddress).GetServiceSummaries();

            services.Add(siloSummary);
        }

        return services.ToArray();
    }

    public async Task<string> StartService(ServiceModel serviceModel)
    {
        return await clusterClient.GetGrain<IServiceControlGrain>(serviceModel.Address)
            .StartService(serviceModel.ServiceName);
    }

    public async Task<string> StopService(ServiceModel serviceModel)
    {
        return await clusterClient.GetGrain<IServiceControlGrain>(serviceModel.Address)
            .StopService(serviceModel.ServiceName);
    }
}

using ServicePanel.Models;
using System.Runtime.Versioning;

namespace ServicePanel.Admin.Services;

[SupportedOSPlatform("windows")]
public class ServiceControlService
{
    private readonly ISiloDetailsProvider siloDetailsProvider;
    private readonly IGrainFactory grainFactory;

    public ServiceControlService(
        ISiloDetailsProvider siloDetailsProvider,
        IGrainFactory grainFactory)
    {
        this.siloDetailsProvider = siloDetailsProvider;
        this.grainFactory = grainFactory;
    }

    public async Task<ServiceModel[]> GetAllServices()
    {
        List<ServiceModel> services = new List<ServiceModel>();

        var allSilos = await siloDetailsProvider.GetSiloDetails();

        foreach (var silloAddress in allSilos.Select(p => p.EndpointAddress))
        {
            var siloServices = await grainFactory.GetGrain<IServiceControl>(silloAddress).GetAllServices();

            services.AddRange(siloServices);
        }

        return services.ToArray();
    }

    public async Task<ServiceModel[]> GetAllServices(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return Array.Empty<ServiceModel>();

        return (await grainFactory.GetGrain<IServiceControl>(address).GetAllServices()).ToArray();
    }

    public async Task<ServiceSummaryModel[]> GetServiceSummaries()
    {
        List<ServiceSummaryModel> services = new List<ServiceSummaryModel>();

        var allSilos = await siloDetailsProvider.GetSiloDetails();

        foreach (var silloAddress in allSilos.Select(p => p.EndpointAddress))
        {
            var siloSummary = await grainFactory.GetGrain<IServiceControl>(silloAddress).GetServiceSummaries();

            services.Add(siloSummary);
        }

        return services.ToArray();
    }

    public async Task<string> StartService(ServiceModel serviceModel)
    {
        return await grainFactory.GetGrain<IServiceControl>(serviceModel.Address)
            .StartService(serviceModel.ServiceName);
    }

    public async Task<string> StopService(ServiceModel serviceModel)
    {
        return await grainFactory.GetGrain<IServiceControl>(serviceModel.Address)
            .StopService(serviceModel.ServiceName);
    }
}

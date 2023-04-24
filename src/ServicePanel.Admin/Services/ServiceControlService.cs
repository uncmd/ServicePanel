using Microsoft.AspNetCore.Components.Forms;
using ServicePanel.Grains;
using ServicePanel.Models;
using System.Runtime.Versioning;

namespace ServicePanel.Admin.Services;

[SupportedOSPlatform("windows")]
public class ServiceControlService
{
    private const string UpdateFileFolder = "UpdateFiles";
    private const long maxFileSize = 1024 * 1024 * 30;

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

    public async Task<ServiceSummaryModel> GetServiceSummary(string address)
    {
        return await clusterClient.GetGrain<IServiceControlGrain>(address).GetServiceSummaries();
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

    public async Task Update(List<ServiceModel> serviceModels, IBrowserFile browserFile)
    {
        foreach (var serviceModel in serviceModels.GroupBy(p => p.Address))
        {
            var serviceNames = serviceModel.Select(p => p.ServiceName).ToList();
            await Update(serviceModel.Key, serviceNames, browserFile);
        }
    }

    public async Task Update(ServiceModel serviceModel, IBrowserFile browserFile)
    {
        var serviceNames = new List<string> { serviceModel.ServiceName };
        await Update(serviceModel.Address, serviceNames, browserFile);
    }

    private async Task Update(string address, List<string> serviceNames, IBrowserFile browserFile)
    {
        var buffers = await BrowserFileToByteArray(browserFile);

        await SaveUpdateFile(buffers, browserFile.Name);

        await clusterClient.GetGrain<IUpdatorGrain>(address)
            .Update(buffers, browserFile.Name, serviceNames.ToArray());
    }

    public async Task UpdateAgent(string address, IBrowserFile browserFile)
    {
        var buffers = await BrowserFileToByteArray(browserFile);

        await SaveUpdateFile(buffers, browserFile.Name);

        await clusterClient.GetGrain<IUpdatorGrain>(address)
            .UpdateAgent(buffers, browserFile.Name);
    }

    public async Task<List<UpdateRecord>> GetUpdateRecords(ServiceModel serviceModel)
    {
        var recordGrain = clusterClient.GetGrain<IUpdateRecordGrain>($"{serviceModel.Address}-{serviceModel.ServiceName}");

        return await recordGrain.GetAll();
    }

    public async Task<List<ServiceLabel>> GetLabels()
    {
        return await clusterClient.GetGrain<IServiceLabelGrain>(0).GetLabels();
    }

    public async Task AddLabel(ServiceLabel label)
    {
        await clusterClient.GetGrain<IServiceLabelGrain>(0).AddLabel(label);
    }

    private static async Task SaveUpdateFile(byte[] buffers, string name)
    {
        string fileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                UpdateFileFolder, DateTime.Now.ToString("yyyyMMddHHmmss"));
        if (!Directory.Exists(fileFolder))
        {
            Directory.CreateDirectory(fileFolder);
        }
        string fileName = Path.Combine(fileFolder, name);

        using var fileStream = new FileStream(fileName, FileMode.Create);
        await fileStream.WriteAsync(buffers);
    }

    private static async Task<byte[]> BrowserFileToByteArray(IBrowserFile browserFile)
    {
        var fileStream = browserFile.OpenReadStream(maxFileSize);
        using (MemoryStream memoryStream = new MemoryStream())
        {
            await fileStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
}

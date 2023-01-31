using Orleans.Statistics;
using ServicePanel.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.ServiceProcess;

namespace ServicePanel.Grains;

[SupportedOSPlatform("windows")]
[PrimaryKeyAddressPlacementStrategy]
public class ServiceControlGrain : Grain, IServiceControlGrain
{
    private readonly IHostEnvironmentStatistics _hostEnvironment;
    private readonly ILogger<ServiceControlGrain> _logger;

    public ServiceControlGrain(
        IHostEnvironmentStatistics hostEnvironment,
        ILogger<ServiceControlGrain> logger)
    {
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public Task<List<ServiceModel>> GetAllServices()
    {
        return Task.FromResult(ServiceManager.GetAllServices().Select(ToServiceModel).ToList());
    }

    public Task<ServiceModel> GetService(string serviceName)
    {
        var service = ServiceManager.GetService(serviceName);
        if (service == null)
            return Task.FromResult(new ServiceModel());

        return Task.FromResult(ToServiceModel(service));
    }

    public Task<string> StartService(string serviceName)
    {
        return Task.FromResult(ServiceManager.StartService(serviceName));
    }

    public Task<string> StopService(string serviceName)
    {
        return Task.FromResult(ServiceManager.StopService(serviceName));
    }

    public Task<string> RestartService(string serviceName)
    {
        return Task.FromResult(ServiceManager.RestartService(serviceName));
    }

    public Task<ServiceSummaryModel> GetServiceSummaries()
    {
        var services = ServiceManager.GetAllServices();

        var summaryModel = new ServiceSummaryModel
        {
            Address = this.GetPrimaryKeyString(),
            Count = services.Count,
            RuningCount = services.Count(p => p.Status == ServiceControllerStatus.Running),
            OSArchitecture = RuntimeInformation.OSArchitecture,
            OSDescription = RuntimeInformation.OSDescription,
        };

        if (_hostEnvironment != null)
        {
            if (_hostEnvironment.CpuUsage.HasValue)
            {
                summaryModel.CpuInfo = $"{(int)(_hostEnvironment.CpuUsage * 100)} %";
            }
            if (_hostEnvironment.AvailableMemory.HasValue && _hostEnvironment.TotalPhysicalMemory.HasValue)
            {
                summaryModel.MemInfo = $"{(_hostEnvironment.TotalPhysicalMemory - _hostEnvironment.AvailableMemory) * 100 / _hostEnvironment.TotalPhysicalMemory} %";
            }
        }

        return Task.FromResult(summaryModel);
    }

    private ServiceModel ToServiceModel(ServiceController serviceController)
    {
        var serviceModel = new ServiceModel
        {
            ServiceName = serviceController.ServiceName,
            DisplayName = serviceController.DisplayName,
            MachineName = serviceController.MachineName,
            StartType = serviceController.StartType,
            Status = serviceController.Status,
            Address = this.GetPrimaryKeyString(),
            CanStop = serviceController.CanStop,
            Pid = serviceController.GetServiceProcessId(),
        };

        try
        {
            var (fileInfo, imagePath) = serviceController.GetWindowsServiceFileInfo();
            serviceModel.DirectoryName = fileInfo?.DirectoryName ?? string.Empty;
            serviceModel.ExePath = fileInfo?.FullName ?? string.Empty;
            serviceModel.UpdateTime = fileInfo?.LastWriteTime;
            serviceModel.ImagePath = imagePath ?? string.Empty;

            if (serviceModel.Pid.HasValue)
            {
                var process = Process.GetProcessById(serviceModel.Pid.Value);
                if (process != null)
                {
                    serviceModel.MemoryInfo = $"{Math.Round((double)process.PrivateMemorySize64 / 1024 / 1024, 1)} MB";
                    serviceModel.ThreadCount = process.Threads.Count.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取服务{ServiceName}安装文件信息失败", serviceController.ServiceName);
        }

        return serviceModel;
    }
}

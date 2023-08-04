using Microsoft.AspNetCore.SignalR.Client;
using Polly;
using Polly.Retry;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.ServiceProcess;

namespace ServicePanel.Agent;

[SupportedOSPlatform("windows")]
public class ServiceControl : IServiceControl
{
    private readonly ILogger<ServiceControl> _logger;
    private readonly string localIP;
    private readonly string UpdateFileFolder = "TempUpdateFile";
    private RetryPolicy fileCopyRetryPolicy;

    public ServiceControl(ILogger<ServiceControl> logger)
    {
        _logger = logger;

        localIP = Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .FirstOrDefault(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .ToString();
    }

    public Task<MachineInfo> GetMachineInfo(List<string> labels)
    {
        labels ??= new List<string>();
        var services = ServiceController.GetServices()
            .Where(p => labels.Any(s => p.ServiceName.Contains(s, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var machineInfo = new MachineInfo
        {
            IP = localIP,
            ServiceCount = services.Count,
            RuningCount = services.Count(p => p.Status == ServiceControllerStatus.Running),
            OSDescription = $"{RuntimeInformation.OSArchitecture} {RuntimeInformation.OSDescription}",
            CpuUsage = EnvironmentStatisticsHosted.CpuUsage,
            TotalPhysicalMemory = EnvironmentStatisticsHosted.TotalPhysicalMemory,
            AvailableMemory = EnvironmentStatisticsHosted.AvailableMemory,
            LastReportTime = DateTime.Now,
            Status = MachineStatus.OnLine,
            ServiceKey = string.Join(",", labels)
        };

        return Task.FromResult(machineInfo);
    }

    public Task<MachineInfo> MachineOffLine()
    {
        var machineInfo = new MachineInfo
        {
            IP = localIP,
            OSDescription = $"{RuntimeInformation.OSArchitecture} {RuntimeInformation.OSDescription}",
            CpuUsage = EnvironmentStatisticsHosted.CpuUsage,
            TotalPhysicalMemory = EnvironmentStatisticsHosted.TotalPhysicalMemory,
            AvailableMemory = EnvironmentStatisticsHosted.AvailableMemory,
            LastReportTime = DateTime.Now,
            Status = MachineStatus.OffLine
        };

        return Task.FromResult(machineInfo);
    }

    public Task<List<ServiceInfo>> GetAllServices(List<string> filters)
    {
        filters ??= new List<string>();
        return Task.FromResult(ServiceController.GetServices()
            .Where(p => filters.Any(s => p.ServiceName.Contains(s, StringComparison.OrdinalIgnoreCase)))
            .Select(ToServiceModel)
            .ToList());
    }

    public Task<ServiceInfo> GetService(string serviceName)
    {
        var service = GetServiceController(serviceName);
        if (service == null)
            return Task.FromResult(new ServiceInfo());

        return Task.FromResult(ToServiceModel(service));
    }

    public Task StartService(string serviceName)
    {
        var controller = GetServiceController(serviceName) ??
            throw new ServicePanelException($"服务{serviceName}启动失败，服务不存在")
                .WithCode(ExceptionCode.ServiceNotFound);

        controller.Start();
        controller.WaitForStatus(ServiceControllerStatus.Running);

        return Task.CompletedTask;
    }

    public Task StopService(string serviceName)
    {
        var controller = GetServiceController(serviceName) ??
            throw new ServicePanelException($"服务{serviceName}停止失败，服务不存在")
                .WithCode(ExceptionCode.ServiceNotFound);

        if (controller.CanStop)
        {
            controller.Stop();
            controller.WaitForStatus(ServiceControllerStatus.Stopped);
        }
        else
        {
            throw new ServicePanelException($"服务{serviceName}当前状态{controller.Status}无法停止")
                .WithCode(ExceptionCode.ServiceCanNotStop);
        }

        return Task.CompletedTask;
    }

    public Task RestartService(string serviceName)
    {
        int timeoutMilliseconds = 50;
        ServiceController service = new ServiceController(serviceName);
        int millisec1 = 0;
        TimeSpan timeout;
        if (service.Status == ServiceControllerStatus.Running)
        {
            millisec1 = Environment.TickCount;
            timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }
        int millisec2 = Environment.TickCount;
        timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));
        service.Start();
        service.WaitForStatus(ServiceControllerStatus.Running, timeout);

        return Task.CompletedTask;
    }

    public async Task<int> Update(byte[] buffers, string fileName, string serviceName)
    {
        // 下载更新文件
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        string zipFileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            UpdateFileFolder, DateTime.Now.ToString("yyyyMMddHHmmss") + fileNameWithoutExtension);
        if (!Directory.Exists(zipFileFolder))
        {
            Directory.CreateDirectory(zipFileFolder);
        }
        var zipFile = Path.Combine(zipFileFolder, fileName);

        using (var fileStream = new FileStream(zipFile, FileMode.Create, FileAccess.Write))
        {
            await fileStream.WriteAsync(buffers);
            await SendLog($"{fileName}下载完成");
        }

        // 解压到临时目录
        string targetDirectory = Path.Combine(zipFileFolder, "extract");
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }
        Decompression(zipFile, targetDirectory);
        await SendLog($"{fileName}解压完成");

        // 停服务 kill process? retry?
        try
        {
            await SendLog($"{serviceName} 开始更新");

            await StopService(serviceName);

            await SendLog($"{serviceName} 已停止");

            // 复制文件到程序目录 文件占用重试??
            var serviceInfo = await GetService(serviceName);
            CopyDirectory(targetDirectory, serviceInfo.DirectoryName);

            await SendLog($"{serviceName} 文件复制完成");

            // 启动服务
            await StartService(serviceName);

            await SendLog($"{serviceName} 已启动");
        }
        catch (ServicePanelException ex) when (ex.Code == ExceptionCode.ServiceNotFound)
        {
            await SendLog($"{serviceName} 更新失败，服务不存在", ex);
            return 0;
        }
        catch (Exception ex)
        {
            await SendLog($"{serviceName} 更新失败", ex);
            return 0;
        }

        var fileCount = GetFilesCount(new DirectoryInfo(targetDirectory));

        // 删除临时目录
        DeleteDirectory(targetDirectory);
        await SendLog("所有服务都更新完成");

        return fileCount;
    }

    public async Task<int> UpdateAgent(byte[] buffers, string fileName)
    {
        // 检查更新程序是否存在
        var agentUpdateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServicePanel.AgentUpdate.exe");
        if (!File.Exists(agentUpdateFile))
        {
            await SendLog("代理更新程序ServicePanel.AgentUpdate.exe不存在");
            return 0;
        }

        // 下载
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        string zipFileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            UpdateFileFolder, DateTime.Now.ToString("yyyyMMddHHmmss") + fileNameWithoutExtension);
        if (!Directory.Exists(zipFileFolder))
        {
            Directory.CreateDirectory(zipFileFolder);
        }
        var zipFile = Path.Combine(zipFileFolder, fileName);

        using var fileStream = new FileStream(zipFile, FileMode.Create, FileAccess.Write);
        await fileStream.WriteAsync(buffers);
        fileStream.Close();
        fileStream.Dispose();
        await SendLog($"{fileName}下载完成");

        // 解压到临时目录
        string targetDirectory = Path.Combine(zipFileFolder, "extract");
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }
        Decompression(zipFile, targetDirectory);
        await SendLog($"{fileName}解压完成");

        await SendLog($"开始执行代理服务更新，脚本参数：{targetDirectory}");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ServicePanel.AgentUpdate.exe",
                Arguments = targetDirectory,
                UseShellExecute = false,
            }
        };

        process.Start();

        var fileCount = GetFilesCount(new DirectoryInfo(targetDirectory));

        await SendLog("代理更新服务已启动");

        return fileCount;
    }

    private ServiceInfo ToServiceModel(ServiceController serviceController)
    {
        var serviceModel = new ServiceInfo
        {
            ServiceName = serviceController.ServiceName,
            DisplayName = serviceController.DisplayName,
            MachineName = serviceController.MachineName,
            StartType = serviceController.StartType,
            Status = serviceController.Status,
            IP = localIP,
            CanStop = serviceController.CanStop,
            Pid = serviceController.GetServiceProcessId(),
            ConnectionId = ServiceControlHosted.connection?.ConnectionId
        };

        try
        {
            var (fileInfo, imagePath) = serviceController.GetWindowsServiceFileInfo();
            serviceModel.DirectoryName = fileInfo?.DirectoryName ?? string.Empty;
            serviceModel.ImageName = fileInfo.Name ?? string.Empty;
            serviceModel.ExePath = fileInfo?.FullName ?? string.Empty;
            serviceModel.UpdateTime = fileInfo?.LastWriteTime;
            serviceModel.ImagePath = imagePath ?? string.Empty;

            if (serviceModel.Pid.HasValue)
            {
                var process = Process.GetProcessById(serviceModel.Pid.Value);
                if (process != null)
                {
                    serviceModel.MemoryInfo = process.PrivateMemorySize64;
                    serviceModel.ThreadCount = process.Threads.Count;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取服务{ServiceName}安装文件信息失败", serviceController.ServiceName);
        }

        return serviceModel;
    }

    private static ServiceController GetServiceController(string serviceName)
    {
        return ServiceController.GetServices()
            .FirstOrDefault(p => p.ServiceName.ToLower() == serviceName.ToLower());
    }

    /// <summary>
    /// 判断一个指定的服务是否正在运行
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    private bool IsServiceRunning(string serviceName)
    {
        ServiceControllerStatus status;
        uint counter = 0;
        do
        {
            ServiceController service = GetServiceController(serviceName);
            if (service == null)
            {
                return false;
            }

            Thread.Sleep(100);
            status = service.Status;
        } while (!(status == ServiceControllerStatus.Stopped ||
                   status == ServiceControllerStatus.Running) &&
                 (++counter < 30));
        return status == ServiceControllerStatus.Running;
    }

    /// <summary>
    /// 判断一个指定的服务是否已安装
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    private bool IsServiceInstalled(string serviceName)
    {
        return GetServiceController(serviceName) != null;
    }

    /// <summary>
    /// 解压文件
    /// </summary>
    /// <param name="targetFile">解压文件路径</param>
    /// <param name="targetDirectory">解压文件后路径</param>
    private static void Decompression(string targetFile, string targetDirectory)
    {
        using var archive = ArchiveFactory.Open(targetFile);
        foreach (var entry in archive.Entries)
        {
            if (!entry.IsDirectory)
            {
                entry.WriteToDirectory(targetDirectory,
                    new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
            }
        }
    }

    private void CopyDirectory(string sourceDirName, string destDirName)
    {
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        if (fileCopyRetryPolicy == null)
        {
            // todo: 可配置重试策略
            int count = 5;
            fileCopyRetryPolicy = Policy.Handle<IOException>()
                .WaitAndRetry(
                    count,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2 4 8 16 32
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogError(exception, "复制文件异常：{TotalSeconds}s后开始重试 {RetryCount}/{Count}", timeSpan.TotalSeconds, retryCount, count);
                    }
                );
        }
        
        foreach (string sourceFileName in Directory.GetFiles(sourceDirName))
        {
            fileCopyRetryPolicy.Execute(() => CopyFileReTry(sourceFileName, destDirName));
        }

        foreach (string dir in Directory.GetDirectories(sourceDirName))
        {
            DirectoryInfo directory = new DirectoryInfo(dir);
            CopyDirectory(dir, Path.Combine(destDirName, directory.Name));
        }
    }

    private void CopyFileReTry(string sourceFileName, string destDirName)
    {
        var fileName = Path.GetFileName(sourceFileName);
        string destFileName = Path.Combine(destDirName, fileName);
        File.Copy(sourceFileName, destFileName, true);
        _logger.LogInformation("{FileName} 复制到 {DestDirName}", fileName, destDirName);
    }

    private void DeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "临时目录{Path}删除失败", path);
        }
    }

    private async Task SendLog(string log, Exception ex = null)
    {
        await SendToAdmin(log, ex);

        if (ex == null)
        {
            _logger.LogInformation(log);
        }
        else
        {
            _logger.LogError(ex, log);
        }
    }

    private async Task SendToAdmin(string log, Exception exception = null)
    {
        try
        {
            UiNotificationEventArgs args = new UiNotificationEventArgs
            {
                Message = exception == null ?
                    $"{DateTime.Now} {log}" :
                    $"{DateTime.Now} {log}{Environment.NewLine}{exception.Message}",
                NotificationType = exception == null ? UiNotificationType.Success : UiNotificationType.Error
            };
            await ServiceControlHosted.connection.InvokeAsync("SendLog", args);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送日志失败");
        }
    }

    public static int GetFilesCount(DirectoryInfo dirInfo)
    {
        int totalFile = 0;
        totalFile += dirInfo.GetFiles().Length;
        foreach (DirectoryInfo subdir in dirInfo.GetDirectories())
        {
            totalFile += GetFilesCount(subdir);
        }
        return totalFile;
    }
}

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.Versioning;

namespace ServicePanel.Admin.Services;

[SupportedOSPlatform("windows")]
public class ServiceControlService
{
    private const string UpdateFileFolder = "UpdateFiles";
    private const long maxFileSize = 1024 * 1024 * 300;
    private readonly IFreeSql _freeSql;
    private readonly IHubContext<ServiceControlHub, IServiceControlClient> _hubContext;
    private readonly MachineHoldService _machineHold;

    public ServiceControlService(IFreeSql freeSql, IHubContext<ServiceControlHub, IServiceControlClient> hubContext, MachineHoldService machineHold)
    {
        _freeSql = freeSql;
        _hubContext = hubContext;
        _machineHold = machineHold;
    }

    public async Task<List<LabelEntity>> GetAllLabels()
    {
        return await _freeSql.Select<LabelEntity>()
            .ToListAsync();
    }

    public async Task<List<LabelEntity>> GetGlobalLabels()
    {
        return await _freeSql.Select<LabelEntity>()
            .Where(p => string.IsNullOrEmpty(p.IP))
            .ToListAsync();
    }

    public async Task AddLabel(LabelEntity labelEntity)
    {
        _ = await _freeSql.Insert(labelEntity)
            .ExecuteAffrowsAsync();
    }

    public async Task RemoveLabel(LabelEntity labelEntity)
    {
        _ = await _freeSql.Delete<LabelEntity>(labelEntity)
            .ExecuteAffrowsAsync();
    }

    public Task<List<MachineInfo>> GetMachineInfos()
    {
        return Task.FromResult(_machineHold.GetMachineInfos());
    }

    public Task<MachineInfo> GetMachineInfo(string ip)
    {
        return Task.FromResult(_machineHold.GetMachineInfo(ip));
    }

    public Task<List<MachineInfo>> GetMachineInfos(string[] ips)
    {
        return Task.FromResult(_machineHold.GetMachineInfos(ips));
    }

    public Task RemoveMachineInfo(MachineInfo machineInfo)
    {
        _machineHold.RemoveMachineInfo(machineInfo);
        return Task.CompletedTask;
    }

    public async Task<List<ServiceInfo>> GetAllServices(string ip, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return new List<ServiceInfo>();

        return await _hubContext.Clients.Client(connectionId).GetServiceInfo();
    }

    public async Task<ControlResult> ControlService(ControlCommand command)
    {
        return await _hubContext.Clients.Client(command.ConnectionId)
            .ControlService(command);
    }

    public async Task Update(ServiceInfo serviceInfo, IBrowserFile browserFile)
    {
        var (buffers, filePath) = await BrowserFileToByteArray(browserFile);

        var command = new UpdateCommand
        {
            Buffers = buffers,
            FileName = browserFile.Name,
            ServiceName = serviceInfo.ServiceName,
            ConnectionId = serviceInfo.ConnectionId
        };

        var result = await _hubContext.Clients.Client(command.ConnectionId)
            .UpdateService(command);

        if (result.Success)
        {
            UpdateEntity updateEntity = new UpdateEntity
            {
                CreateTime = DateTime.Now,
                IP = serviceInfo.IP,
                ServiceName = serviceInfo.ServiceName,
                Size = browserFile.Size,
                FileName = browserFile.Name,
                FilePath = filePath,
                TotalFiles = int.Parse(result.Message)
            };
            await _freeSql.Insert(updateEntity).ExecuteAffrowsAsync();
        }
    }

    public async Task UpdateAgent(MachineInfo machineInfo, IBrowserFile browserFile)
    {
        var (buffers, filePath) = await BrowserFileToByteArray(browserFile);

        var command = new UpdateCommand
        {
            Buffers = buffers,
            FileName = browserFile.Name,
            ConnectionId = machineInfo.ConnectionId
        };

        var result = await _hubContext.Clients.Client(command.ConnectionId)
            .UpdateAgent(command);

        if (result.Success)
        {
            UpdateEntity updateEntity = new UpdateEntity
            {
                CreateTime = DateTime.Now,
                IP = machineInfo.IP,
                ServiceName = "Windows管理工具代理服务",
                Size = browserFile.Size,
                FileName = browserFile.Name,
                FilePath = filePath,
                TotalFiles = int.Parse(result.Message)
            };
            await _freeSql.Insert(updateEntity).ExecuteAffrowsAsync();
        }
    }

    public async Task<List<UpdateEntity>> GetUpdateRecords(ServiceInfo[] serviceInfo)
    {
        var ips = serviceInfo.Select(p => p.IP).Distinct();
        var services = serviceInfo.Select(p => p.ServiceName).Distinct();
        return await _freeSql.Select<UpdateEntity>()
            .Where(p => ips.Contains(p.IP))
            .Where(p => services.Contains(p.ServiceName))
            .OrderByDescending(p => p.CreateTime)
            .ToListAsync();
    }

    public async Task<List<UpdateEntity>> GetUpdateRecords(MachineInfo[] serviceInfo)
    {
        var ips = serviceInfo.Select(p => p.IP).Distinct();
        return await _freeSql.Select<UpdateEntity>()
            .Where(p => ips.Contains(p.IP))
            .Where(p => p.ServiceName == "Windows管理工具代理服务")
            .OrderByDescending(p => p.CreateTime)
            .ToListAsync();
    }

    /// <summary>
    /// 回滚到指定版本
    /// </summary>
    /// <param name="updateEntity"></param>
    /// <returns></returns>
    public async Task Rollback(UpdateEntity updateEntity)
    {
        var machineInfo = _machineHold.GetMachineInfo(updateEntity.IP);
        var buffers = await File.ReadAllBytesAsync(updateEntity.FilePath);

        var command = new UpdateCommand
        {
            Buffers = buffers,
            FileName = updateEntity.FileName,
            ConnectionId = machineInfo.ConnectionId,
            ServiceName = updateEntity.ServiceName,
        };

        await _hubContext.Clients.Client(command.ConnectionId)
            .UpdateService(command);
    }

    private static async Task<(byte[], string)> BrowserFileToByteArray(IBrowserFile browserFile)
    {
        var fileStream = browserFile.OpenReadStream(maxFileSize);
        using (MemoryStream memoryStream = new MemoryStream())
        {
            await fileStream.CopyToAsync(memoryStream);
            var buffers = memoryStream.ToArray();
            var filePath = await SaveUpdateFile(buffers, browserFile.Name);
            return (buffers, filePath);
        }
    }

    private static async Task<string> SaveUpdateFile(byte[] buffers, string name)
    {
        string fileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                UpdateFileFolder, DateTime.Now.ToString("yyyyMMddHHmmss"));
        if (!Directory.Exists(fileFolder))
        {
            Directory.CreateDirectory(fileFolder);
        }
        string filePath = Path.Combine(fileFolder, name);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await fileStream.WriteAsync(buffers);
        return filePath;
    }
}

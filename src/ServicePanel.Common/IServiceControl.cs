using System.Runtime.Versioning;

namespace ServicePanel;

[SupportedOSPlatform("windows")]
public interface IServiceControl
{
    Task<MachineInfo> GetMachineInfo(List<string> labels);

    Task<MachineInfo> MachineOffLine();

    Task<List<ServiceInfo>> GetAllServices(List<string> filters);

    Task<ServiceInfo> GetService(string serviceName);

    Task StartService(string serviceName);

    Task StopService(string serviceName);

    Task RestartService(string serviceName);

    Task<int> Update(byte[] buffers, string fileName, string serviceName);

    Task<int> UpdateAgent(byte[] buffers, string fileName);
}
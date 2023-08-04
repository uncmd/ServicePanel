namespace ServicePanel.Admin.Services;

public class MachineHoldService
{
    private readonly List<MachineInfo> machines = new();

    public void InsertOrUpdate(MachineInfo machine)
    {
        if (machines.Any(p => p.IP == machine.IP))
        {
            var originMachine = machines.FirstOrDefault(p => p.IP == machine.IP);
            originMachine.LastReportTime = machine.LastReportTime;
            originMachine.ServiceCount = machine.ServiceCount;
            originMachine.RuningCount = machine.RuningCount;
            originMachine.CpuUsage = machine.CpuUsage;
            originMachine.AvailableMemory = machine.AvailableMemory;
            originMachine.ConnectionId = machine.ConnectionId;
            originMachine.Status = machine.Status;
            originMachine.ServiceKey = machine.ServiceKey;
        }
        else
        {
            machines.Add(machine);
        }
    }

    public List<MachineInfo> GetMachineInfos()
    {
        machines.RemoveAll(p => p.Status == MachineStatus.OffLine && p.LastReportTime < DateTime.Now.AddDays(-3));

        return machines;
    }

    public MachineInfo GetMachineInfo(string ip)
    {
        return machines.FirstOrDefault(p => p.IP == ip);
    }

    public List<MachineInfo> GetMachineInfos(string[] ips)
    {
        return machines.Where(p => ips.Contains(p.IP)).ToList();
    }

    public bool RemoveMachineInfo(MachineInfo machineInfo)
    {
        return machines.Remove(machineInfo);
    }
}

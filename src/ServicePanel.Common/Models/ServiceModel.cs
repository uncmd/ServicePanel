using System.ServiceProcess;

namespace ServicePanel.Models;

[GenerateSerializer]
public class ServiceModel
{
    [Id(0)]
    public string ServiceName { get; set; }

    [Id(1)]
    public string DisplayName { get; set; }

    [Id(2)]
    public string MachineName { get; set; }

    [Id(3)]
    public ServiceControllerStatus Status { get; set; }

    [Id(4)]
    public ServiceStartMode StartType { get; set; }

    [Id(5)]
    public string Address { get; set; }

    [Id(6)]
    public string DirectoryName { get; set; }

    [Id(7)]
    public string ExePath { get; set; }

    [Id(8)]
    public string ImagePath { get; set; }

    [Id(9)]
    public int? Pid { get; set; }

    [Id(10)]
    public bool CanStop { get; set; }

    [Id(11)]
    public DateTime? UpdateTime { get; set; }

    [Id(12)]
    public string MemoryInfo { get; set; }

    [Id(13)]
    public string ThreadCount { get; set; }

    public bool Processing { get; set; }

    public bool CanStart => StartType != ServiceStartMode.Disabled && Status == ServiceControllerStatus.Stopped;

    public bool CanRestart => StartType != ServiceStartMode.Disabled && Status == ServiceControllerStatus.Running;
}
using System.Runtime.InteropServices;

namespace ServicePanel.Models;

[GenerateSerializer]
public class ServiceSummaryModel
{
    [Id(0)]
    public string Address { get; set; }

    [Id(1)]
    public int Count { get; set; }

    [Id(2)]
    public int RuningCount { get; set; }

    [Id(3)]
    public Architecture OSArchitecture { get; set; }

    [Id(4)]
    public string OSDescription { get; set; }

    [Id(5)]
    public string MemInfo { get; set; }

    [Id(6)]
    public string CpuInfo { get; set; }

    [Id(7)]
    public string DeviceInfo { get; set; }
}

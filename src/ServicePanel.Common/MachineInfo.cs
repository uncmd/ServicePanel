namespace ServicePanel;

/// <summary>
/// 机器基本信息
/// </summary>
public class MachineInfo
{
    /// <summary>
    /// 主机地址
    /// </summary>
    public string IP { get; set; }

    /// <summary>
    /// 操作系统信息
    /// </summary>
    public string OSDescription { get; set; }

    /// <summary>
    /// 最后报告时间
    /// </summary>
    public DateTime LastReportTime { get; set; }

    /// <summary>
    /// 服务数量
    /// </summary>
    public int ServiceCount { get; set; }

    /// <summary>
    /// 运行中的服务数量
    /// </summary>
    public int RuningCount { get; set; }

    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; }

    /// <summary>
    /// 在线状态
    /// </summary>
    public MachineStatus Status { get; set; }

    /// <summary>
    /// 服务过滤配置
    /// </summary>
    public string ServiceKey { get; set; }

    public bool ShowDetails { get; set; }
}

public enum MachineStatus
{
    OnLine,

    OffLine
}

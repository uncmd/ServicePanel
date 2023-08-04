using System.ServiceProcess;

namespace ServicePanel;

/// <summary>
/// 服务基本信息
/// </summary>
public class ServiceInfo
{
    /// <summary>
    /// 名称
    /// </summary>
    public string ServiceName { get; set; }

    /// <summary>
    /// IP
    /// </summary>
    public string IP { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// 机器名称
    /// </summary>
    public string MachineName { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ServiceControllerStatus Status { get; set; }

    /// <summary>
    /// 启动模式
    /// </summary>
    public ServiceStartMode StartType { get; set; }

    /// <summary>
    /// 服务路径
    /// </summary>
    public string DirectoryName { get; set; }

    /// <summary>
    /// 主文件名称
    /// </summary>
    public string ImageName { get; set; }

    /// <summary>
    /// 主文件路径
    /// </summary>
    public string ExePath { get; set; }

    /// <summary>
    /// 命令行
    /// </summary>
    public string ImagePath { get; set; }

    /// <summary>
    /// PID
    /// </summary>
    public int? Pid { get; set; }

    /// <summary>
    /// 是否运行停止服务
    /// </summary>
    public bool CanStop { get; set; }

    /// <summary>
    /// 主文件更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 内存
    /// </summary>
    public long MemoryInfo { get; set; }

    /// <summary>
    /// 线程数量
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// 是否正在处理
    /// </summary>
    public bool Processing { get; set; }

    /// <summary>
    /// 是否允许启动
    /// </summary>
    public bool CanStart => StartType != ServiceStartMode.Disabled && Status == ServiceControllerStatus.Stopped;

    /// <summary>
    /// 是否允许重启
    /// </summary>
    public bool CanRestart => StartType != ServiceStartMode.Disabled && Status == ServiceControllerStatus.Running;

    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; }
}
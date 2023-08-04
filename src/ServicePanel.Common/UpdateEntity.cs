namespace ServicePanel;

/// <summary>
/// 更新信息
/// </summary>
public class UpdateEntity
{
    /// <summary>
    /// 版本号
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string IP { get; set; }

    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 更新文件
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// 文件数
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// 文件大小
    /// </summary>
    public long Size { get; set; }
}

namespace ServicePanel;

/// <summary>
/// 标签信息
/// </summary>
public class LabelEntity
{
    /// <summary>
    /// 标签名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 当 <see cref="LabelType"/> 为Global时为空
    /// </summary>
    public string IP { get; set; }
}

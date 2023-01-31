using ServicePanel.Models;

namespace ServicePanel.Grains;

/// <summary>
/// 更新记录 主键：地址+服务名称
/// </summary>
public interface IUpdateRecordGrain : IGrainWithStringKey
{
    Task Add(UpdateRecord record);

    Task<List<UpdateRecord>> GetAll();
}

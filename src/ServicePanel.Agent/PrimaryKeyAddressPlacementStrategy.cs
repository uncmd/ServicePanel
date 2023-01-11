using Orleans.Placement;
using Orleans.Runtime;

namespace ServicePanel;

[Serializable]
public sealed class PrimaryKeyAddressPlacementStrategy : PlacementStrategy
{
}

/// <summary>
/// 键值为Silo地址的放置策略
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PrimaryKeyAddressPlacementStrategyAttribute : PlacementAttribute
{
    public PrimaryKeyAddressPlacementStrategyAttribute() :
        base(new PrimaryKeyAddressPlacementStrategy())
    {
    }
}
using Orleans.Runtime;
using Orleans.Runtime.Placement;

namespace ServicePanel;

public sealed class PrimaryKeyAddressPlacementDirector : IPlacementDirector
{
    public Task<SiloAddress> OnAddActivation(
        PlacementStrategy strategy,
        PlacementTarget target,
        IPlacementContext context)
    {
        // 多网卡？
        var allSilos = context.GetCompatibleSilos(target);

        var targetAddress = target.GrainIdentity.Key.ToString();
        var targetSilo = allSilos.FirstOrDefault(p => p.Endpoint.Address.ToString() == targetAddress);
        if (targetSilo != null)
        {
            return Task.FromResult(targetSilo);
        }

        return Task.FromResult(allSilos[Random.Shared.Next(allSilos.Length)]);
    }
}

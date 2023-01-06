using Orleans.Runtime;
using ServicePanel.Models;

namespace ServicePanel;

public sealed class SiloStatusOracleSiloDetailsProvider : ISiloDetailsProvider
{
    private readonly ISiloStatusOracle siloStatusOracle;

    public SiloStatusOracleSiloDetailsProvider(ISiloStatusOracle siloStatusOracle)
    {
        this.siloStatusOracle = siloStatusOracle;
    }

    public Task<SiloDetails[]> GetSiloDetails()
    {
        return Task.FromResult(siloStatusOracle.GetApproximateSiloStatuses(true)
            .Select(x => new SiloDetails
            {
                Status = x.Value.ToString(),
                SiloStatus = x.Value,
                SiloAddress = x.Key.ToParsableString(),
                SiloName = x.Key.ToParsableString(), // Use the address for naming
                EndpointAddress = x.Key.Endpoint.Address.ToString()
            })
            .GroupBy(p => p.EndpointAddress).Select(p => p.First()).ToArray());
    }
}

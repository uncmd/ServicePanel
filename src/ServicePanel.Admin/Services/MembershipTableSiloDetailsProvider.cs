using Orleans.Runtime;
using ServicePanel.Models;
using System.Globalization;

namespace ServicePanel;

public sealed class MembershipTableSiloDetailsProvider : ISiloDetailsProvider
{
    private readonly IClusterClient clusterClient;

    public MembershipTableSiloDetailsProvider(IClusterClient clusterClient)
    {
        this.clusterClient = clusterClient;
    }

    public async Task<SiloDetails[]> GetSiloDetails()
    {
        //default implementation uses managementgrain details
        var grain = clusterClient.GetGrain<IManagementGrain>(0);

        var hosts = await grain.GetDetailedHosts(true);

        return hosts.Select(x => new SiloDetails
        {
            FaultZone = x.FaultZone,
            HostName = x.HostName,
            IAmAliveTime = ToISOString(x.IAmAliveTime),
            ProxyPort = x.ProxyPort,
            RoleName = x.RoleName,
            SiloAddress = x.SiloAddress.ToParsableString(),
            SiloName = x.SiloName,
            StartTime = ToISOString(x.StartTime),
            Status = x.Status.ToString(),
            SiloStatus = x.Status,
            UpdateZone = x.UpdateZone,
            EndpointAddress = x.SiloAddress.Endpoint.Address.ToString()
        }).GroupBy(p => p.EndpointAddress).Select(p => p.First()).ToArray();
    }

    private static string ToISOString(DateTime value)
    {
        return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
    }
}

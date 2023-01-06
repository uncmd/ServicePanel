using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Runtime.Placement;
using ServicePanel.Statistics;
using System.Runtime.InteropServices;

namespace ServicePanel;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServicePanel(this IServiceCollection services)
    {
        services.AddSingletonNamedService<
            PlacementStrategy, PrimaryKeyAddressPlacementStrategy>(nameof(PrimaryKeyAddressPlacementStrategy));
        services.AddSingletonKeyedService<
            Type, IPlacementDirector, PrimaryKeyAddressPlacementDirector>(
                typeof(PrimaryKeyAddressPlacementStrategy));

        services.AddSingleton<SiloStatusOracleSiloDetailsProvider>();
        services.AddSingleton<MembershipTableSiloDetailsProvider>();
        services.AddSingleton<ISiloDetailsProvider>(c =>
        {
            var membershipTable = c.GetService<IMembershipTable>();
            if (membershipTable != null)
            {
                return c.GetRequiredService<MembershipTableSiloDetailsProvider>();
            }
            return c.GetRequiredService<SiloStatusOracleSiloDetailsProvider>();
        });

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsEnvironmentStatisticsServices.RegisterServices<ISiloLifecycle>(services);
        }

        return services;
    }
}

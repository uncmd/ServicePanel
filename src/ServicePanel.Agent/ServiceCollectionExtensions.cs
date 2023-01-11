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

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsEnvironmentStatisticsServices.RegisterServices<ISiloLifecycle>(services);
        }

        services.AddHttpClient();

        return services;
    }
}

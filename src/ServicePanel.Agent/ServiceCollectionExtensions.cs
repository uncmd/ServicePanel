using ServicePanel.Agent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ServicePanel;

public static class ServiceCollectionExtensions
{
    public static IHostBuilder AddServicePanel(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(service => service.AddServicePanel());

        return hostBuilder;
    }

    public static IServiceCollection AddServicePanel(this IServiceCollection services)
    {
        Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        services.AddOptions<ServicePanelAgentOptions>().BindConfiguration(ServicePanelAgentOptions.Section);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IServiceControl, ServiceControl>();
            services.AddHostedService<ServiceControlHosted>();
            services.AddSingleton<HeartbeatReport>();
        }

        return services;
    }
}

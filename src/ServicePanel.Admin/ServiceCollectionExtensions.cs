using ServicePanel.Admin.Services;
using System.Runtime.InteropServices;

namespace ServicePanel.Admin;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServicePanel(this IServiceCollection services)
    {
        services.AddSingleton<ISiloDetailsProvider, MembershipTableSiloDetailsProvider>();
        services.AddScoped<LayoutService>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<ServiceControlService>();
        }

        return services;
    }
}

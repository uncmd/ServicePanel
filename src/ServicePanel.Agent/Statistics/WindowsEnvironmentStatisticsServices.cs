using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration.Internal;
using Orleans.Statistics;
using System.Runtime.InteropServices;

namespace ServicePanel.Statistics;

internal class WindowsEnvironmentStatisticsServices
{
    /// <summary>
    /// Registers <see cref="WindowsEnvironmentStatistics"/> services.
    /// </summary>
    internal static void RegisterServices<TLifecycleObservable>(IServiceCollection services) where TLifecycleObservable : ILifecycleObservable
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        if (!isWindows)
        {
            var logger = services.BuildServiceProvider().GetService<ILogger<WindowsEnvironmentStatistics>>();
            logger?.LogWarning($"无效的操作系统类型 {RuntimeInformation.OSDescription}");

            return;
        }

        services.AddSingleton<WindowsEnvironmentStatistics>();
        services.AddFromExisting<IHostEnvironmentStatistics, WindowsEnvironmentStatistics>();
        services.AddSingleton<ILifecycleParticipant<TLifecycleObservable>, WindowsEnvironmentStatisticsLifecycleAdapter<TLifecycleObservable>>();
    }
}

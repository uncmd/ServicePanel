using CZGL.SystemInfo;
using Microsoft.Extensions.Logging;
using Orleans.Statistics;

namespace ServicePanel.Statistics;

internal class WindowsEnvironmentStatistics : IHostEnvironmentStatistics, ILifecycleObserver, IDisposable
{
    /// <inheritdoc />
    public long? TotalPhysicalMemory { get; private set; }

    /// <inheritdoc />
    public float? CpuUsage { get; private set; }

    /// <inheritdoc />
    public long? AvailableMemory { get; private set; }

    private readonly TimeSpan MONITOR_PERIOD = TimeSpan.FromSeconds(5);

    private CancellationTokenSource _cts;
    private Task _monitorTask;

    private readonly ILogger _logger;
    private const float KB = 1024f;

    public WindowsEnvironmentStatistics(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<WindowsEnvironmentStatistics>();
    }

    public void Dispose()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }

    public async Task OnStart(CancellationToken ct)
    {
        _logger.LogTrace($"Starting {nameof(WindowsEnvironmentStatistics)}");

        _cts = new CancellationTokenSource();
        ct.Register(() => _cts.Cancel());

        _monitorTask = await Task.Factory.StartNew(
            () => Monitor(_cts.Token),
            _cts.Token,
            TaskCreationOptions.DenyChildAttach | TaskCreationOptions.RunContinuationsAsynchronously,
            TaskScheduler.Default
        );

        _logger.LogTrace($"Started {nameof(WindowsEnvironmentStatistics)}");
    }

    public async Task OnStop(CancellationToken ct)
    {
        if (_cts == null)
            return;

        _logger.LogTrace($"Stopping {nameof(WindowsEnvironmentStatistics)}");
        try
        {
            _cts.Cancel();
            try
            {
                await _monitorTask;
            }
            catch (TaskCanceledException) { }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error stopping {nameof(WindowsEnvironmentStatistics)}");
        }
        finally
        {
            _logger.LogTrace($"Stopped {nameof(WindowsEnvironmentStatistics)}");
        }
    }

    private async Task Monitor(CancellationToken ct)
    {
        int i = 0;
        while (true)
        {
            if (ct.IsCancellationRequested)
                throw new TaskCanceledException("Monitor task canceled");

            try
            {
                await Task.WhenAll(
                    UpdateCpuUsage(i),
                    UpdatePhysicalMemory()
                );

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    var logStr = $"WindowsEnvironmentStatistics: CpuUsage={CpuUsage?.ToString("0.0")}, TotalPhysicalMemory={TotalPhysicalMemory}, AvailableMemory={AvailableMemory}";
                    _logger.LogTrace(logStr);
                }

                await Task.Delay(MONITOR_PERIOD, ct);
            }
            catch (Exception ex) when (ex.GetType() != typeof(TaskCanceledException))
            {
                _logger.LogError(ex, "WindowsEnvironmentStatistics: error");
                await Task.Delay(MONITOR_PERIOD + MONITOR_PERIOD + MONITOR_PERIOD, ct);
            }
            if (i < 2)
                i++;
        }
    }

    private CPUTime _prevTime;
    private Task UpdateCpuUsage(int i)
    {
        var currentTime = CPUHelper.GetCPUTime();

        if (i > 0)
        {
            CpuUsage = (float)CPUHelper.CalculateCPULoad(_prevTime, currentTime);
        }

        _prevTime = currentTime;

        return Task.CompletedTask;
    }

    private Task UpdatePhysicalMemory()
    {
        var memoryValue = MemoryHelper.GetMemoryValue();

        TotalPhysicalMemory ??= (long)memoryValue.TotalPhysicalMemory;

        AvailableMemory = (long)memoryValue.AvailablePhysicalMemory;

        return Task.CompletedTask;
    }
}

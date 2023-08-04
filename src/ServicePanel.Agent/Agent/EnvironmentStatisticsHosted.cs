namespace ServicePanel.Agent;

public class EnvironmentStatisticsHosted : IDisposable, IHostedService
{
    public static long TotalPhysicalMemory { get; private set; }

    public static float CpuUsage { get; private set; }

    public static long AvailableMemory { get; private set; }

    private readonly TimeSpan MONITOR_PERIOD = TimeSpan.FromSeconds(3);

    private CancellationTokenSource _cts;
    private Task _monitorTask;

    private readonly ILogger _logger;
    private const float KB = 1024f;

    public EnvironmentStatisticsHosted(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EnvironmentStatisticsHosted>();
    }

    public void Dispose()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogTrace($"Starting {nameof(EnvironmentStatisticsHosted)}");

        _cts = new CancellationTokenSource();
        ct.Register(() => _cts.Cancel());

        _monitorTask = await Task.Factory.StartNew(
            () => Monitor(_cts.Token),
            _cts.Token,
            TaskCreationOptions.DenyChildAttach | TaskCreationOptions.RunContinuationsAsynchronously,
            TaskScheduler.Default
        );

        _logger.LogTrace($"Started {nameof(EnvironmentStatisticsHosted)}");
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_cts == null)
            return;

        _logger.LogTrace($"Stopping {nameof(EnvironmentStatisticsHosted)}");
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
            _logger.LogError(ex, $"Error stopping {nameof(EnvironmentStatisticsHosted)}");
        }
        finally
        {
            _logger.LogTrace($"Stopped {nameof(EnvironmentStatisticsHosted)}");
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
                    var logStr = $"WindowsEnvironmentStatistics: CpuUsage={CpuUsage.ToString("0.0")}, TotalPhysicalMemory={TotalPhysicalMemory}, AvailableMemory={AvailableMemory}";
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
        var (totalMemory, availableMemory) = MemoryHelper.GetMemory();

        TotalPhysicalMemory = (long)totalMemory;

        AvailableMemory = (long)availableMemory;

        return Task.CompletedTask;
    }
}

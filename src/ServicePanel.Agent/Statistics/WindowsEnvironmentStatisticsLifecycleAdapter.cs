namespace ServicePanel.Statistics;

internal class WindowsEnvironmentStatisticsLifecycleAdapter<TLifecycle>
    : ILifecycleParticipant<TLifecycle>, ILifecycleObserver where TLifecycle : ILifecycleObservable
{
    private readonly WindowsEnvironmentStatistics _statistics;

    public WindowsEnvironmentStatisticsLifecycleAdapter(WindowsEnvironmentStatistics statistics)
    {
        _statistics = statistics;
    }

    public Task OnStart(CancellationToken ct) => _statistics.OnStart(ct);

    public Task OnStop(CancellationToken ct) => _statistics.OnStop(ct);

    public void Participate(TLifecycle lifecycle)
    {
        lifecycle.Subscribe(
            nameof(WindowsEnvironmentStatistics),
            ServiceLifecycleStage.RuntimeInitialize,
            this);
    }
}

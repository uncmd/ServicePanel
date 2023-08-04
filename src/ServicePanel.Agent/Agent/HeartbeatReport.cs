using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;

namespace ServicePanel.Agent;

/// <summary>
/// 心跳发送
/// </summary>
[SupportedOSPlatform("windows")]
public class HeartbeatReport
{
    private readonly PeriodicTimer timer;
    private readonly IServiceControl _serviceControl;
    private readonly ILogger<HeartbeatReport> _logger;
    private HubConnection connection;

    public HeartbeatReport(
        IServiceControl serviceControl, 
        ILogger<HeartbeatReport> logger,
        IOptions<ServicePanelAgentOptions> options)
    {
        _serviceControl = serviceControl;
        _logger = logger;

        timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.HeartbeatIntervalSeconds));
    }

    public async Task Start(HubConnection connection, List<string> filter, CancellationToken cancellationToken)
    {
        _logger.LogInformation("启动心跳发送组件");
        this.connection = connection;

        await Heartbeat(filter, cancellationToken);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await Heartbeat(filter, cancellationToken);
        }
    }

    public async Task ReportOffLine(CancellationToken cancellationToken = default)
    {
        try
        {
            var machineInfo = await _serviceControl.MachineOffLine();
            machineInfo.ConnectionId = "";

            await connection.InvokeAsync("Heartbeat", machineInfo, cancellationToken);

            _logger.LogInformation("{IP}下线成功", machineInfo.IP);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下线失败");
        }
    }

    private async Task Heartbeat(List<string> filter, CancellationToken cancellationToken)
    {
        try
        {
            var machineInfo = await _serviceControl.GetMachineInfo(filter);
            machineInfo.ConnectionId = connection.ConnectionId;

            await connection.InvokeAsync("Heartbeat", machineInfo, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("心跳发送成功");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "心跳发送启动异常");
        }
    }
}

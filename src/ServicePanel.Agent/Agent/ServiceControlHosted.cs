using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Polly;
using System.Runtime.Versioning;

namespace ServicePanel.Agent;

[SupportedOSPlatform("windows")]
internal class ServiceControlHosted : IServiceControlClient, IHostedService
{
    private readonly IServiceControl _serviceControl;
    private readonly ILogger<ServiceControlHosted> _logger;
    private readonly HeartbeatReport _heartbeatReport;
    private readonly ServicePanelAgentOptions _options;
    public static HubConnection connection;
    private readonly string adminUrl;
    private const int DefaultBufferSize = 10 * 1024 * 1024;
    private readonly List<string> _filters;

    public ServiceControlHosted(
        IServiceControl serviceControl,
        ILogger<ServiceControlHosted> logger,
        IOptions<ServicePanelAgentOptions> options,
        HeartbeatReport heartbeatReport)
    {
        _serviceControl = serviceControl;
        _logger = logger;
        _heartbeatReport = heartbeatReport;
        _options = options.Value;
        _filters = _options.ServiceKey.Split(",").ToList();

        adminUrl = _options.AdminUrl + "/service-control-hub";
        connection = new HubConnectionBuilder()
            .WithUrl(adminUrl, options =>
            {
                options.ApplicationMaxBufferSize = DefaultBufferSize;
                options.TransportMaxBufferSize = DefaultBufferSize;
                options.WebSocketConfiguration = conf =>
                {
                    conf.RemoteCertificateValidationCallback = (message, cert, chain, errors) => { return true; };
                };
                options.HttpMessageHandlerFactory = factory => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                };
            })
            .WithAutomaticReconnect(new RandomRetryPolicy(_logger, adminUrl))
            .AddMessagePackProtocol()
            .Build();

        InitOnMethod();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            RetryStartSignalR(cancellationToken);
        }, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _heartbeatReport.ReportOffLine(cancellationToken);
        await connection.StopAsync(cancellationToken);
    }

    /// <summary>
    /// 接收控制面板调用的方法
    /// </summary>
    private void InitOnMethod()
    {
        connection.On<ControlCommand, ControlResult>(nameof(IServiceControlClient.ControlService), ControlService);
        connection.On(nameof(IServiceControlClient.GetServiceInfo), GetServiceInfo);
        connection.On<UpdateCommand, ControlResult>(nameof(IServiceControlClient.UpdateService), UpdateService);
        connection.On<UpdateCommand, ControlResult>(nameof(IServiceControlClient.UpdateAgent), UpdateAgent);
    }

    private Task RetryStartSignalR(CancellationToken cancellationToken)
    {
        return Policy.Handle<Exception>()
            .WaitAndRetryForeverAsync(retryAttempt => retryAttempt < 6 ?
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) :
                TimeSpan.FromSeconds(60),
            (exception, timeSpan) =>
            {
                _logger.LogError(exception, "连接到中心{AdminUrl}失败：{TotalSeconds}s后开始重试",
                    adminUrl, timeSpan.TotalSeconds);
            })
            .ExecuteAsync(async () =>
            {
                await connection.StartAsync(cancellationToken);
                _logger.LogInformation("连接到中心{AdminUrl}成功", adminUrl);
                await _heartbeatReport.Start(connection, _filters, cancellationToken);
            });
    }

    public async Task<ControlResult> ControlService(ControlCommand command)
    {
        if (command == null)
        {
            _logger.LogError("control common cannot be empty");
            return ControlResult.Error("control common cannot be empty");
        }

        _logger.LogInformation("开始执行控制服务命令：{ControlCommand}", command);

        try
        {
            if (command.ControlType == ControlType.Start)
            {
                await _serviceControl.StartService(command.ServiceName);
            }
            else if (command.ControlType == ControlType.Stop)
            {
                await _serviceControl.StopService(command.ServiceName);
            }
            else if (command.ControlType == ControlType.ReStart)
            {
                await _serviceControl.RestartService(command.ServiceName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "控制服务失败: {ControlCommand}", command);
            return ControlResult.Error(ex.Message);
        }

        return ControlResult.OK;
    }

    public async Task<List<ServiceInfo>> GetServiceInfo()
    {
        return await _serviceControl.GetAllServices(_filters);
    }

    public async Task<ControlResult> UpdateService(UpdateCommand command)
    {
        if (command == null)
        {
            _logger.LogError("control common cannot be empty");
            return ControlResult.Error("control common cannot be empty");
        }

        _logger.LogInformation("开始执行更新服务命令：{UpdateCommand}", command);

        int fileCount;
        try
        {
            fileCount =  await _serviceControl.Update(command.Buffers, command.FileName, command.ServiceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新服务失败: {UpdateCommand}", command);
            return ControlResult.Error(ex.Message);
        }

        return new ControlResult() { Success = true, Message = fileCount.ToString() };
    }

    public async Task<ControlResult> UpdateAgent(UpdateCommand command)
    {
        if (command == null)
        {
            _logger.LogError("control common cannot be empty");
            return ControlResult.Error("control common cannot be empty");
        }

        _logger.LogInformation("开始执行更新代理命令：{UpdateCommand}", command);

        int fileCount;
        try
        {
            fileCount = await _serviceControl.UpdateAgent(command.Buffers, command.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新代理失败: {UpdateCommand}", command);
            return ControlResult.Error(ex.Message);
        }

        return new ControlResult() { Success = true, Message = fileCount.ToString() };
    }

    internal sealed class RandomRetryPolicy : IRetryPolicy
    {
        private readonly ILogger logger;
        private readonly string adminUrl;

        public RandomRetryPolicy(ILogger logger, string adminUrl)
        {
            this.logger = logger;
            this.adminUrl = adminUrl;
        }

        internal static TimeSpan?[] DEFAULT_RETRY_DELAYS_IN_MILLISECONDS = new TimeSpan?[]
        {
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
        };

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            TimeSpan? retryTimeSpan;
            if (retryContext.PreviousRetryCount < DEFAULT_RETRY_DELAYS_IN_MILLISECONDS.Length)
            {
                retryTimeSpan = DEFAULT_RETRY_DELAYS_IN_MILLISECONDS[retryContext.PreviousRetryCount];
            }
            else
            {
                retryTimeSpan = TimeSpan.FromSeconds(Random.Shared.NextDouble() * 10);
            }

            if (retryContext.RetryReason != null)
                logger.LogError(retryContext.RetryReason, "连接失败");

            logger.LogInformation("{RetryReason}，开始重新连接到{AdminUrl}，第{PreviousRetryCount}次重试, 耗时: {ElapsedTime}, {NextRetryDelay}秒后开始下一次重试",
                retryContext.RetryReason?.Message, adminUrl, retryContext.PreviousRetryCount + 1, retryContext.ElapsedTime, retryTimeSpan.Value.TotalSeconds);

            return retryTimeSpan;
        }
    }
}

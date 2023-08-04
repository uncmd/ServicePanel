using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Runtime.Versioning;
using System.ServiceProcess;

namespace ServicePanel.AgentUpdate;

[SupportedOSPlatform("windows")]
public class AgentUpdate
{
    private static HubConnection connection;

    public static async Task Update(string sourcePath, string serviceName)
    {
        Log.Information("连接到管理控制台");

        await StartConnection();

        await SendLog("开始更新代理");

        try
        {
            if (!Directory.Exists(sourcePath))
            {
                await SendLog($"更新文件目录不存在：{sourcePath}");
                return;
            }

            var allServices = ServiceController.GetServices();
            var agentService = allServices.FirstOrDefault(p => p.ServiceName == serviceName);

            if (agentService == null)
            {
                foreach (var service in allServices)
                {
                    var (fileInfo, imagePath) = service.GetWindowsServiceFileInfo();
                    if (fileInfo.Name == "ServicePanel.Agent.exe")
                    {
                        agentService = service;
                        break;
                    }
                }

                if (agentService == null)
                {
                    await SendLog($"代理服务{serviceName}不存在，无法更新");
                    return;
                }
            }

            if (agentService.CanStop)
            {
                agentService.Stop();
                agentService.WaitForStatus(ServiceControllerStatus.Stopped);
                await SendLog("代理已停止");
            }

            CopyFolder(sourcePath, AppDomain.CurrentDomain.BaseDirectory);

            await SendLog("文件复制完成");

            agentService.Start();
            agentService.WaitForStatus(ServiceControllerStatus.Running);

            await SendLog("代理已启动，更新完成");

            // 删除临时目录
            DeleteDirectory(sourcePath);
        }
        catch (Exception ex)
        {
            await SendLog("更新代理发生异常", ex);
        }
        finally
        {
            await connection?.StopAsync();
        }
    }

    /// <summary>
    /// 复制文件夹及文件
    /// </summary>
    /// <param name="sourceFolder">原文件路径</param>
    /// <param name="destFolder">目标文件路径</param>
    /// <returns></returns>
    static int CopyFolder(string sourceFolder, string destFolder)
    {
        try
        {
            //如果目标路径不存在,则创建目标路径
            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }
            //得到原文件根目录下的所有文件
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest, true);
                Log.Information("copy {name}", name);
            }
            //得到原文件根目录下的所有文件夹
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest);//递归复制文件
            }
            return 1;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 0;
        }

    }

    private static void DeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "临时目录{Path}删除失败", path);
        }
    }

    private static async Task SendLog(string log, Exception ex = null)
    {
        await SendToAdmin(log, ex);

        if (ex == null)
        {
            Log.Information(log);
        }
        else
        {
            Log.Error(ex, log);
        }
    }

    private static async Task SendToAdmin(string log, Exception exception = null)
    {
        try
        {
            UiNotificationEventArgs args = new UiNotificationEventArgs
            {
                Message = exception == null ?
                    $"{DateTime.Now} {log}" :
                    $"{DateTime.Now} {log}{Environment.NewLine}{exception.Message}",
                NotificationType = exception == null ? UiNotificationType.Success : UiNotificationType.Error
            };
            await connection.InvokeAsync("SendLog", args);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Log.Error(ex, "发送日志失败");
        }
    }

    private static async Task StartConnection()
    {
        try
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();
            var adminUrl = configuration["Agent:AdminUrl"] + "/service-control-hub";
            connection = new HubConnectionBuilder()
                .WithUrl(adminUrl, options =>
                {
                    options.WebSocketConfiguration = conf =>
                    {
                        conf.RemoteCertificateValidationCallback = (message, cert, chain, errors) => { return true; };
                    };
                    options.HttpMessageHandlerFactory = factory => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                    };
                })
                .WithAutomaticReconnect(new RandomRetryPolicy(adminUrl))
                .AddMessagePackProtocol()
                .Build();

            await connection.StartAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动连接失败");
        }
    }

    internal sealed class RandomRetryPolicy : IRetryPolicy
    {
        private readonly string adminUrl;

        public RandomRetryPolicy(string adminUrl)
        {
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

            Log.Information("{RetryReason}，开始重新连接到{AdminUrl}，第{PreviousRetryCount}次重试, 耗时: {ElapsedTime}, {NextRetryDelay}秒后开始下一次重试",
                retryContext.RetryReason?.Message, adminUrl, retryContext.PreviousRetryCount + 1, retryContext.ElapsedTime, retryTimeSpan.Value.TotalSeconds);

            return retryTimeSpan;
        }
    }
}

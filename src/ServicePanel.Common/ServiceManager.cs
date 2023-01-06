using System.Runtime.Versioning;
using System.ServiceProcess;

namespace ServicePanel;

/// <summary>
/// Windows服务管理类
/// </summary>
[SupportedOSPlatform("windows")]
public class ServiceManager
{
    /// <summary>
    /// 获取当前Windows操作系统所有的服务列表
    /// </summary>
    /// <returns></returns>
    public static List<ServiceController> GetAllServices()
    {
        return ServiceController.GetServices().ToList();
    }

    /// <summary>
    /// 获取单个指定服务名称的Windows服务
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    public static ServiceController? GetService(string serviceName)
    {
        return GetAllServices().FirstOrDefault(_ => _.ServiceName.ToLower() == serviceName.ToLower());
    }

    /// <summary>
    /// 判断一个指定的服务是否正在运行
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    public static bool IsServiceRunning(string serviceName)
    {
        ServiceControllerStatus status;
        uint counter = 0;
        do
        {
            ServiceController? service = GetService(serviceName);
            if (service == null)
            {
                return false;
            }

            Thread.Sleep(100);
            status = service.Status;
        } while (!(status == ServiceControllerStatus.Stopped ||
                   status == ServiceControllerStatus.Running) &&
                 (++counter < 30));
        return status == ServiceControllerStatus.Running;
    }

    /// <summary>
    /// 判断一个指定的服务是否已安装
    /// </summary>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    public static bool IsServiceInstalled(string serviceName)
    {
        return GetService(serviceName) != null;
    }

    /// <summary>
    /// 启动一个指定名称的服务
    /// </summary>
    /// <param name="serviceName"></param>
    public static string StartService(string serviceName)
    {
        var controller = GetService(serviceName);
        if (controller == null)
        {
            return $"服务{serviceName}启动失败，服务不存在";
        }

        try
        {
            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running);

            return $"服务{serviceName}启动成功";
        }
        catch (Exception ex)
        {
            return $"服务{serviceName}启动失败: {ExceptionReport(ex)}";
        }
    }

    /// <summary>
    /// 停止一个指定名称的服务
    /// </summary>
    /// <param name="serviceName"></param>
    public static string StopService(string serviceName)
    {
        var controller = GetService(serviceName);
        if (controller == null)
        {
            return $"服务{serviceName}停止失败，服务不存在";
        }

        try
        {
            controller.Stop();
            controller.WaitForStatus(ServiceControllerStatus.Stopped);

            return $"服务{serviceName}停止成功";
        }
        catch (Exception ex)
        {
            return $"服务{serviceName}停止失败: {ExceptionReport(ex)}";
        }
    }

    /// <summary>
    /// 重启一个指定名称的服务
    /// </summary>
    /// <param name="serviceName"></param>
    /// <remarks>重启一个指定名称的服务需要首先找到服务，如果此服务正在运行，则需要停止，然后再启动服务</remarks>
    public static string RestartService(string serviceName)
    {
        int timeoutMilliseconds = 50;
        ServiceController service = new ServiceController(serviceName);
        try
        {
            int millisec1 = 0;
            TimeSpan timeout;
            if (service.Status == ServiceControllerStatus.Running)
            {
                millisec1 = Environment.TickCount;
                timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            int millisec2 = Environment.TickCount;
            timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));
            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, timeout);

            return $"服务{serviceName}重启成功";
        }
        catch (Exception ex)
        {
            return $"服务{serviceName}重启失败: {ExceptionReport(ex)}";
        }
    }

    private static string ExceptionReport(Exception ex)
    {
        string result = ex.Message;
        if (ex.InnerException != null)
        {
            result += $"  {ExceptionReport(ex.InnerException)}";
        }
        return result;
    }
}

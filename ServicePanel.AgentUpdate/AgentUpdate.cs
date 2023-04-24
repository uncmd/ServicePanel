using Serilog;
using System.Runtime.Versioning;
using System.ServiceProcess;

namespace ServicePanel.AgentUpdate;

[SupportedOSPlatform("windows")]
public class AgentUpdate
{
    public static void Update(string sourcePath, string serviceName)
    {
        Log.Information("开始更新代理服务");
        Log.Information("更新文件目录：" + sourcePath);

        var agentService = ServiceController.GetServices().FirstOrDefault(p => p.ServiceName == serviceName);

        if (agentService == null)
        {
            Log.Information($"代理服务{serviceName}不存在");
            return;
        }

        if (agentService.CanStop)
        {
            agentService.Stop();
            agentService.WaitForStatus(ServiceControllerStatus.Stopped);
            Log.Information("代理服务已停止");
        }

        CopyFolder(sourcePath, AppDomain.CurrentDomain.BaseDirectory);

        Log.Information("文件已更新");

        agentService.Start();
        agentService.WaitForStatus(ServiceControllerStatus.Running);

        Log.Information("代理服务已启动");
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
}

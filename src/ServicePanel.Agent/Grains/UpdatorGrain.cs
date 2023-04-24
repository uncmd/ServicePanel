using Orleans.Utilities;
using Polly;
using Polly.Retry;
using ServicePanel.Models;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace ServicePanel.Grains;

[SupportedOSPlatform("windows")]
[PrimaryKeyAddressPlacementStrategy]
public class UpdatorGrain : Grain, IUpdatorGrain
{
    private readonly string UpdateFileFolder = "TempUpdateFile";

    private readonly ILogger<UpdatorGrain> logger;
    private readonly AsyncRetryPolicy fileUsedRetryPolicy;

    private readonly ObserverManager<IChat> _subsManager;

    public UpdatorGrain(
        ILogger<UpdatorGrain> logger)
    {
        this.logger = logger;

        // todo: 可配置重试策略
        int count = 5;
        fileUsedRetryPolicy = Policy.Handle<IOException>()
            .WaitAndRetryAsync(
                count,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2 4 8 16 32
                async (exception, timeSpan, retryCount, context) =>
                {
                    await WriteMessage($"复制文件异常：{exception.Message}，{timeSpan.TotalSeconds}s后开始重试 {retryCount}/{count}");
                }
            );

        _subsManager = new ObserverManager<IChat>(TimeSpan.FromMinutes(5), logger);
    }

    public async Task Update(byte[] buffers, string fileName, string[] serviceNames)
    {
        try
        {
            // 下载
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string zipFileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                UpdateFileFolder, DateTime.Now.ToString("yyyyMMddHHmmss") + fileNameWithoutExtension);
            if (!Directory.Exists(zipFileFolder))
            {
                Directory.CreateDirectory(zipFileFolder);
            }
            var zipFile = Path.Combine(zipFileFolder, fileName);

            using var fileStream = new FileStream(zipFile, FileMode.Create, FileAccess.Write);
            await fileStream.WriteAsync(buffers);
            fileStream.Close();
            fileStream.Dispose();
            await WriteMessage("更新文件下载完成");

            // 解压到临时目录
            string targetDirectory = Path.Combine(zipFileFolder, "extract");
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
            Decompression(zipFile, targetDirectory);
            await WriteMessage("更新文件解压完成");

            var totalFiles = GetFilesCount(new DirectoryInfo(targetDirectory));

            var address = this.GetPrimaryKeyString();
            var serviceControl = GrainFactory.GetGrain<IServiceControlGrain>(address);
            foreach (var serviceName in serviceNames)
            {
                // 停服务
                var stopResult = await serviceControl.StopService(serviceName);
                await WriteMessage(stopResult);
                // kill process?

                // 复制 文件占用重试??
                var serviceModel = await serviceControl.GetService(serviceName);
                await CopyDirectory(targetDirectory, serviceModel.DirectoryName);

                // 启动服务
                var startResult = await serviceControl.StartService(serviceName);
                await WriteMessage(startResult);

                // 保存更新记录
                await SaveRecord(address, serviceName, totalFiles);
            }

            // 删除临时目录
            DeleteDirectory(zipFileFolder);
            await WriteMessage("更新完成");
        }
        catch (Exception ex)
        {
            await WriteMessage($"文件{fileName}更新失败", ex);
            throw;
        }
    }

    public async Task UpdateAgent(byte[] buffers, string fileName)
    {
        try
        {
            // 下载
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string zipFileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                UpdateFileFolder, DateTime.Now.ToString("yyyyMMddHHmmss") + fileNameWithoutExtension);
            if (!Directory.Exists(zipFileFolder))
            {
                Directory.CreateDirectory(zipFileFolder);
            }
            var zipFile = Path.Combine(zipFileFolder, fileName);

            using var fileStream = new FileStream(zipFile, FileMode.Create, FileAccess.Write);
            await fileStream.WriteAsync(buffers);
            fileStream.Close();
            fileStream.Dispose();
            await WriteMessage("代理更新文件下载完成");

            // 解压到临时目录
            string targetDirectory = Path.Combine(zipFileFolder, "extract");
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
            Decompression(zipFile, targetDirectory);
            await WriteMessage("更新文件解压完成");

            await WriteMessage($"开始执行更新脚本：ServiceUpdate.ps1");
            await WriteMessage($"脚本参数：{targetDirectory}");
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ServicePanel.AgentUpdate.exe",
                    Arguments = targetDirectory,
                    UseShellExecute = false,
                }
            };

            process.Start();

            // 保存更新记录
            var totalFiles = GetFilesCount(new DirectoryInfo(targetDirectory));
            await SaveRecord("", "代理服务", totalFiles);
        }
        catch (Exception ex)
        {
            await WriteMessage($"代理更新失败", ex);
            throw;
        }
    }

    /// <summary>
    /// 解压文件
    /// </summary>
    /// <param name="targetFile">解压文件路径</param>
    /// <param name="targetDirectory">解压文件后路径</param>
    public static void Decompression(string targetFile, string targetDirectory)
    {
        var archive = ArchiveFactory.Open(targetFile);
        foreach (var entry in archive.Entries)
        {
            if (!entry.IsDirectory)
            {
                entry.WriteToDirectory(targetDirectory,
                    new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
            }
        }
    }

    private async Task CopyDirectory(string sourceDirName, string destDirName)
    {
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        foreach (string sourceFileName in Directory.GetFiles(sourceDirName))
        {
            await fileUsedRetryPolicy.ExecuteAsync(async () => await CopyFileReTry(sourceFileName, destDirName));
        }
    }

    private async Task CopyFileReTry(string sourceFileName, string destDirName)
    {
        var fileName = Path.GetFileName(sourceFileName);
        string destFileName = Path.Combine(destDirName, fileName);
        File.Copy(sourceFileName, destFileName, true);
        await WriteMessage($"{fileName} 复制到 {destDirName}");
    }

    private async Task WriteMessage(string message, Exception ex = null)
    {
        await _subsManager.Notify(s => s.ReceiveMessage($"{DateTime.Now} {message}"));
        if (ex == null)
        {
            logger.LogInformation(message);
        }
        else
        {
            await _subsManager.Notify(s => s.ReceiveMessage($"{DateTime.Now} {ex.Message}"));
            logger.LogError(ex, message);
        }
    }

    private void DeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "删除目录 {Path} 失败", path);
        }
    }

    private async Task SaveRecord(string address, string serviceName, int totalFiles)
    {
        UpdateRecord record = new UpdateRecord
        {
            Address = address,
            ServiceName = serviceName,
            UpdateTime = DateTime.Now,
            TotalFiles = totalFiles
        };

        var recordGrain = GrainFactory.GetGrain<IUpdateRecordGrain>($"{address}-{serviceName}");
        await recordGrain.Add(record);
    }

    private int GetFilesCount(DirectoryInfo dirInfo)
    {
        int totalFiles = 0;
        totalFiles += dirInfo.GetFiles().Length;
        foreach (var subDir in dirInfo.GetDirectories())
        {
            totalFiles += GetFilesCount(subDir);
        }
        return totalFiles;
    }

    /// <summary>
    /// 客户端调用它来订阅
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    public Task Subscribe(IChat observer)
    {
        _subsManager.Subscribe(observer, observer);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 客户端使用此选项取消订阅，不再接收消息
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    public Task UnSubscribe(IChat observer)
    {
        _subsManager.Unsubscribe(observer);

        return Task.CompletedTask;
    }
}

using Orleans.Utilities;
using Polly;
using Polly.Retry;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Runtime.Versioning;

namespace ServicePanel.Grains;

[SupportedOSPlatform("windows")]
[PrimaryKeyAddressPlacementStrategy]
public class UpdatorGrain : Grain, IUpdatorGrain
{
    private readonly string UpdateFileFolder = "TempUpdateFile";

    private readonly ILogger<UpdatorGrain> logger;
    private readonly RetryPolicy fileUsedRetryPolicy;

    private readonly ObserverManager<IChat> _subsManager;

    public UpdatorGrain(ILogger<UpdatorGrain> logger)
    {
        this.logger = logger;

        fileUsedRetryPolicy = Policy.Handle<IOException>(ex => ex.Message.Contains("used by another process"))
            .WaitAndRetry(
                5,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2 4 8 16 32
                (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(exception, "复制文件异常，{TotalSeconds}s后开始重试 {RetryCount}/3", timeSpan.TotalSeconds, retryCount);
                }
            );

        _subsManager = new ObserverManager<IChat>(TimeSpan.FromMinutes(5), logger);
    }

    public async Task Update(byte[] buffers, string fileName, string[] serviceNames)
    {
        try
        {
            // 下载
            string zipFileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                UpdateFileFolder, Guid.NewGuid().ToString("N"));
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

            var serviceControl = GrainFactory.GetGrain<IServiceControlGrain>(this.GetPrimaryKeyString());
            foreach (var serviceName in serviceNames)
            {
                // 停服务
                var stopResult = await serviceControl.StopService(serviceName);
                await WriteMessage(stopResult);
                // kill process?

                // 复制 文件占用重试??
                var serviceModel = await serviceControl.GetService(serviceName);
                CopyDirectory(targetDirectory, serviceModel.DirectoryName);

                // 启动服务
                var startResult = await serviceControl.StartService(serviceName);
                await WriteMessage(startResult);
            }

            // 删除临时目录
            fileUsedRetryPolicy.Execute(() => DeleteDirectory(zipFileFolder));
            await WriteMessage("更新完成");
        }
        catch (Exception ex)
        {
            await WriteMessage($"文件{fileName}更新失败", ex);
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

    private void CopyDirectory(string sourceDirName, string destDirName)
    {
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        foreach (string sourceFileName in Directory.GetFiles(sourceDirName))
        {
            fileUsedRetryPolicy.Execute(() => CopyFileReTry(sourceFileName, destDirName));
        }
    }

    private async void CopyFileReTry(string sourceFileName, string destDirName)
    {
        string destFileName = Path.Combine(destDirName, Path.GetFileName(sourceFileName));
        File.Copy(sourceFileName, destFileName, true);
        await WriteMessage($"{sourceFileName} 复制到 {destFileName}");
    }

    private Task WriteMessage(string message, Exception ex = null)
    {
        _subsManager.Notify(s => s.ReceiveMessage($"{DateTime.Now} {this.GetPrimaryKeyString()} {message}"));
        if (ex == null)
        {
            logger.LogInformation(message);
        }
        else
        {
            _subsManager.Notify(s => s.ReceiveMessage($"{DateTime.Now} {this.GetPrimaryKeyString()} {ex.Message}"));
            logger.LogError(ex, message);
        }
        return Task.CompletedTask;
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

    // Clients call this to subscribe.
    public Task Subscribe(IChat observer)
    {
        _subsManager.Subscribe(observer, observer);

        return Task.CompletedTask;
    }

    //Clients use this to unsubscribe and no longer receive messages.
    public Task UnSubscribe(IChat observer)
    {
        _subsManager.Unsubscribe(observer);

        return Task.CompletedTask;
    }
}

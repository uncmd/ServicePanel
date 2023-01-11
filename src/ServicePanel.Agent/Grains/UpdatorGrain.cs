using Microsoft.Extensions.Logging;
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
    private readonly IHttpClientFactory clientFactory;

    public UpdatorGrain(
        ILogger<UpdatorGrain> logger, 
        IHttpClientFactory clientFactory)
    {
        this.logger = logger;
        this.clientFactory = clientFactory;
    }

    public async Task Update(string updateAddress, string filePath, string fileName, string[] serviceNames)
    {
        try
        {
            // 下载
            string zipFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                UpdateFileFolder, Guid.NewGuid().ToString("N"));

            var localFile = Path.Combine(zipFilePath, fileName);
            var file = new FileInfo(localFile);
            await DownloadFile(updateAddress, filePath, file);

            // 解压到临时目录
            string targetDirectory = Path.Combine(zipFilePath, "extract");
            Decompression(file.FullName, targetDirectory);

            var serviceControl = GrainFactory.GetGrain<IServiceControlGrain>(this.GetPrimaryKeyString());
            foreach (var serviceName in serviceNames)
            {
                // 停服务
                var stopResult = await serviceControl.StopService(serviceName);
                logger.LogInformation(stopResult);
                // kill process?

                // 复制 文件占用重试??
                var serviceModel = await serviceControl.GetService(serviceName);
                CopyDirectory(targetDirectory, serviceModel.ExePath);

                // 启动服务
                var startResult = await serviceControl.StartService(serviceName);
                logger.LogInformation(startResult);
            }

            // 删除临时目录
            DeleteDirectory(zipFilePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "更新失败，请检查更新地址和更新文件: {UpdateAddress}", updateAddress);
            throw;
        }
    }

    private async Task DownloadFile(string baseAddress, string path, FileInfo file)
    {
        if (!baseAddress.EndsWith("/") && !baseAddress.EndsWith("\\"))
            baseAddress += "/";

        var fileUrlPath = baseAddress + path;
        var client = clientFactory.CreateClient();
        var response = await client.GetAsync(fileUrlPath);

        try
        {
            var stream = await response.Content.ReadAsStreamAsync();
            using (var fileStream = file.Create())
            using (stream)
            {
                await stream.CopyToAsync(fileStream);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "文件 {File} 下载失败", fileUrlPath);
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
        List<string> stringList = new List<string>();
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        foreach (string sourceFileName in Directory.GetFiles(sourceDirName))
        {
            string destFileName = Path.Combine(destDirName, Path.GetFileName(sourceFileName));
            File.Copy(sourceFileName, destFileName, true);
            logger.LogInformation("{SourceFileName} 复制到 {DestFileName}", sourceFileName, destFileName);
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
}

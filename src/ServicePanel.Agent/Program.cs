using Serilog;
using ServicePanel;

try
{
    Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

    // 单文件发布需要显式传递配置程序集
    var configurationAssemblies = new[]
    {
        typeof(ConsoleLoggerConfigurationExtensions).Assembly,
        typeof(FileLoggerConfigurationExtensions).Assembly,
        typeof(LoggerConfigurationAsyncExtensions).Assembly,
    };

    IHost host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(services =>
        {
            services.AddServicePanel();
        })
        .UseSerilog((context, config) =>
        {
            config
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(context.Configuration, configurationAssemblies);
        })
        .UseOrleans((context, builder) =>
        {
            builder.UseRedisClustering("10.10.2.118:6379,password=mscsredis123!!", 3);
        })
        .UseWindowsService()
        .Build();

    Log.Information("Starting ServicePanel.Agent.");
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    if (ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
    {
        throw;
    }

    Log.Fatal(ex, "ServicePanel.Agent terminated unexpectedly!");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

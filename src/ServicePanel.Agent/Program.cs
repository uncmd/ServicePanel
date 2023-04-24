using Orleans.Clustering.Redis;
using Serilog;
using ServicePanel;

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/error.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    new Mutex(true, "ServicePanel.Agent", out bool createdNew);
    if (!createdNew)
    {
        logger.Warning("ServicePanel代理正在运行，请不要重复运行.");
        return 1;
    }

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
            var redisOptions = context.Configuration
                .GetSection("RedisClusteringOptions")
                .Get<RedisClusteringOptions>();

            if (redisOptions == null)
            {
                Log.Error("未配置redis，请添加RedisClusteringOptions配置项");
                throw new ArgumentException("未配置redis，请添加RedisClusteringOptions配置项");
            }
            builder
                .UseRedisClustering(redisOptions.ConnectionString, redisOptions.Database)
                .AddRedisGrainStorageAsDefault(options =>
                {
                    options.ConnectionString = redisOptions.ConnectionString;
                    options.DatabaseNumber = redisOptions.Database;
                });
        })
        .UseWindowsService()
        .Build();

    logger.Information("Starting ServicePanel.Agent.");
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    if (ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
    {
        throw;
    }

    logger.Fatal(ex, "ServicePanel.Agent terminated unexpectedly!");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

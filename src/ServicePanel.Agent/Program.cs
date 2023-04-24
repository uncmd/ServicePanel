using Orleans.Clustering.Redis;
using Serilog;
using ServicePanel;

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/logs.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
var serviceName = "ServicePanelAgent";

try
{
    new Mutex(true, serviceName, out bool createdNew);
    if (!createdNew)
    {
        logger.Warning("{ServiceName}�����������У��벻Ҫ�ظ�����.", serviceName);
        return 1;
    }

    Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

    // ���ļ�������Ҫ��ʽ�������ó���
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
                logger.Error("δ����redis�������RedisClusteringOptions������");
                throw new ArgumentException("δ����redis�������RedisClusteringOptions������");
            }
            builder
                .UseRedisClustering(redisOptions.ConnectionString, redisOptions.Database)
                .AddRedisGrainStorageAsDefault(options =>
                {
                    options.ConnectionString = redisOptions.ConnectionString;
                    options.DatabaseNumber = redisOptions.Database;
                });
        })
        .UseWindowsService(options =>
        {
            options.ServiceName = serviceName;
        })
        .Build();

    logger.Information("Starting {ServiceName}.", serviceName);
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    if (ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
    {
        throw;
    }

    logger.Fatal(ex, "{ServiceName} terminated unexpectedly!", serviceName);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

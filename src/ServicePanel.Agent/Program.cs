using Serilog;
using ServicePanel;

Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
#if DEBUG
    .WriteTo.Async(c => c.Console())
#endif
    .WriteTo.Async(c => c.File("Logs/logs.txt", rollingInterval: RollingInterval.Day))
    .CreateLogger();
var serviceName = "ServicePanelAgent";

try
{
    IHost host = Host.CreateDefaultBuilder(args)
        .AddServicePanel()
        .UseSerilog()
        .UseWindowsService(options =>
        {
            options.ServiceName = serviceName;
        })
        .Build();

    Log.Information("Starting {ServiceName}.", serviceName);
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    if (ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
    {
        throw;
    }

    Log.Fatal(ex, "{ServiceName} terminated unexpectedly!", serviceName);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

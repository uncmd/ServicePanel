using Serilog;
using ServicePanel.AgentUpdate;
using System.Runtime.InteropServices;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Async(c => c.Console())
    .WriteTo.Async(c => c.File("Logs/agentUpdate.txt"))
    .CreateLogger();

var sourcePath = args[0];
var serviceName = "ServicePanelAgent";

try
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        AgentUpdate.Update(sourcePath, serviceName);
    }
    else
    {
        Log.Error($"Not Supported OSPlatform: {RuntimeInformation.OSDescription}");
    }

    return 0;
}
catch (Exception ex)
{
    if (ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
    {
        throw;
    }

    Log.Fatal(ex, "terminated unexpectedly!");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

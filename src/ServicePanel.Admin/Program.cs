using MudBlazor;
using MudBlazor.Services;
using Orleans.Clustering.Redis;
using Serilog;
using ServicePanel.Admin;

Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

try
{
    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddServicePanel();
    builder.Services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    });

    var redisOptions = builder.Configuration
        .GetSection("RedisClusteringOptions")
        .Get<RedisClusteringOptions>();

    if (redisOptions == null)
    {
        Log.Error("Î´ÅäÖÃredis£¬ÇëÌí¼ÓRedisClusteringOptionsÅäÖÃÏî");
        return 1;
    }

    builder.Host
        .UseSerilog()
        .UseOrleansClient(builder =>
        {
            builder.UseRedisClustering(redisOptions.ConnectionString, redisOptions.Database);
        })
        .UseWindowsService();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseRouting();

    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    Log.Information("Starting ServicePanel.Admin.");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    if (ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
    {
        throw;
    }

    Log.Fatal(ex, "ServicePanel.Admin terminated unexpectedly!");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

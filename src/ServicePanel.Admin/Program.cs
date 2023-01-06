using MudBlazor;
using MudBlazor.Services;
using Serilog;
using ServicePanel;
using ServicePanel.Admin.Services;
using System.Runtime.InteropServices;

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
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    });

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        builder.Services.AddSingleton<ServiceControlService>();
    }

    builder.Host
        .UseSerilog()
        .UseOrleans((context, builder) =>
        {
            builder.UseRedisClustering("10.10.2.118:6379,password=mscsredis123!!", 3);
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
    return 1;
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

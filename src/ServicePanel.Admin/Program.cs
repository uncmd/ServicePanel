using Microsoft.AspNetCore.ResponseCompression;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using ServicePanel.Admin;
using ServicePanel.Admin.Services;

Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
#if DEBUG
    .WriteTo.Async(o => o.Console())
#endif
    .WriteTo.Async(o => o.File("Logs/logs.txt", rollingInterval: RollingInterval.Day))
    .CreateLogger();

try
{
    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddSignalR(options =>
    {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
    }).AddMessagePackProtocol();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddServicePanel();
    builder.Services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    });
    
    builder.Host
        .UseSerilog()
        .UseWindowsService();

    builder.Services.AddResponseCompression(opts =>
    {
        opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
              new[] { "application/octet-stream" });
    });

    var app = builder.Build();

    app.UseResponseCompression();
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
    app.MapHub<ServiceControlHub>("/service-control-hub");
    app.MapFallbackToPage("/control-panel/{address?}", "/_Host");
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

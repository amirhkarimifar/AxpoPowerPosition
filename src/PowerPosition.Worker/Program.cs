using Microsoft.Extensions.Hosting.WindowsServices;
using PowerPosition.Core;
using PowerPosition.Worker;
using Serilog;
using Serilog.Events;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : null
});

builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
{
    ["--output"] = "Extract:OutputFolder",
    ["--interval"] = "Extract:IntervalMinutes",
    ["--timezone"] = "Extract:TimeZoneId",
    ["--day-offset"] = "Extract:DayOffset"
});

builder.Services.AddWindowsService(options => options.ServiceName = "PowerPosition.Worker");

var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "logs");

builder.Logging.ClearProviders();
builder.Services.AddSerilog(configuration => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}")
    .WriteTo.File(
        Path.Combine(logDirectory, "powerposition-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

builder.Services.AddPowerPositionCore();
builder.Services.AddHostedService<ExtractSchedulerService>();

try
{
    Console.Title = "PowerPosition.Worker";
}
catch (IOException)
{
}

var host = builder.Build();

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
var dashboardProcess = DashboardLauncher.TryStart(
    builder.Environment.ContentRootPath,
    host.Services.GetRequiredService<ILogger<Program>>(),
    lifetime.ApplicationStopping);
if (dashboardProcess is not null)
{
    DashboardLauncher.StopOnShutdown(dashboardProcess, lifetime);
}

host.Run();

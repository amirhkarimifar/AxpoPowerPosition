using PowerPosition.Dashboard.Components;
using PowerPosition.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var defaultLogsFolder = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "PowerPosition.Worker", "logs"));
var defaultOutputFolder = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "PowerPosition.Worker", "output"));

var logsFolder = builder.Configuration["Monitor:LogsFolder"] is { Length: > 0 } logsOverride
    ? Path.GetFullPath(logsOverride)
    : defaultLogsFolder;
var outputFolder = builder.Configuration["Monitor:OutputFolder"] is { Length: > 0 } outputOverride
    ? Path.GetFullPath(outputOverride)
    : defaultOutputFolder;

builder.Services.AddSingleton(new MonitorPaths(logsFolder, outputFolder));
builder.Services.AddScoped<LogFileService>();
builder.Services.AddScoped<OutputFileService>();

var launchedByWorker = builder.Configuration["POWERPOSITION_LAUNCHED_BY_WORKER"] == "true";
builder.Services.AddSingleton(new LaunchContext(launchedByWorker));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

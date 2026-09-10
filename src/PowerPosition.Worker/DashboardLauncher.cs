using System.ComponentModel;
using System.Diagnostics;

namespace PowerPosition.Worker;

public static class DashboardLauncher
{
    private const string DashboardUrl = "http://localhost:5078";
    private const string LaunchedByWorkerEnvVar = "POWERPOSITION_LAUNCHED_BY_WORKER";
    private static readonly TimeSpan ReadyPollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);

    public static Process? TryStart(string workerContentRoot, ILogger logger, CancellationToken shutdownToken)
    {
        var dashboardProjectPath = Path.GetFullPath(Path.Combine(workerContentRoot, "..", "PowerPosition.Dashboard"));
        if (!Directory.Exists(dashboardProjectPath))
        {
            return null;
        }

        try
        {
            var startInfo = new ProcessStartInfo("dotnet", $"run --no-launch-profile --urls {DashboardUrl}")
            {
                WorkingDirectory = dashboardProjectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.Environment[LaunchedByWorkerEnvVar] = "true";

            var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            process.OutputDataReceived += (_, _) => { };
            process.ErrorDataReceived += (_, _) => { };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            logger.LogInformation("Dashboard starting; waiting for it to accept connections before announcing the address");
            _ = AnnounceWhenReadyAsync(logger, shutdownToken);

            return process;
        }
        catch (Exception ex) when (ex is IOException or Win32Exception)
        {
            logger.LogWarning(ex, "Could not start the dashboard ({Reason}); the extract schedule is unaffected", ex.Message);
            return null;
        }
    }

    private static async Task AnnounceWhenReadyAsync(ILogger logger, CancellationToken shutdownToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow + ReadyTimeout;

        while (DateTime.UtcNow < deadline && !shutdownToken.IsCancellationRequested)
        {
            try
            {
                var response = await client.GetAsync(DashboardUrl, shutdownToken);
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Dashboard ready at {DashboardUrl} — open it in your browser", DashboardUrl);
                    PrintBanner();
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
            }

            try
            {
                await Task.Delay(ReadyPollInterval, shutdownToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        if (!shutdownToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Dashboard did not become reachable at {DashboardUrl} within {TimeoutSeconds}s; it may still be starting, or may have failed to start",
                DashboardUrl, ReadyTimeout.TotalSeconds);
        }
    }

    private static void PrintBanner()
    {
        Console.WriteLine();
        var originalBackground = Console.BackgroundColor;
        var originalForeground = Console.ForegroundColor;

        Console.BackgroundColor = ConsoleColor.Cyan;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write(" DASHBOARD ");
        Console.BackgroundColor = originalBackground;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($" {DashboardUrl}");

        Console.ForegroundColor = originalForeground;
        Console.WriteLine();
    }

    public static void StopOnShutdown(Process process, IHostApplicationLifetime lifetime)
    {
        lifetime.ApplicationStopping.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
            }
        });
    }
}

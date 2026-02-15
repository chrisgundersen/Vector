using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;
using Xunit;

namespace Vector.Web.UITests.Infrastructure;

/// <summary>
/// Fixture that starts both the API and Underwriting Dashboard for E2E testing.
/// Uses actual process hosting for realistic testing.
/// </summary>
public class TestServerFixture : IAsyncLifetime
{
    private Process? _apiProcess;
    private Process? _dashboardProcess;

    public string ApiBaseUrl { get; private set; } = null!;
    public string DashboardBaseUrl { get; private set; } = null!;

    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    private static readonly string SolutionRoot = FindSolutionRoot();

    public async Task InitializeAsync()
    {
        // Find available ports
        var apiPort = GetAvailablePort();
        var dashboardPort = GetAvailablePort();

        ApiBaseUrl = $"http://localhost:{apiPort}";
        DashboardBaseUrl = $"http://localhost:{dashboardPort}";

        // Start the API
        _apiProcess = StartProcess(
            Path.Combine(SolutionRoot, "src", "Vector.Api"),
            apiPort,
            "Vector.Api");

        // Start the Dashboard
        _dashboardProcess = StartProcess(
            Path.Combine(SolutionRoot, "src", "Vector.Web.Underwriting"),
            dashboardPort,
            "Vector.Web.Underwriting");

        // Wait for both servers to be ready
        await WaitForServerAsync(ApiBaseUrl, "API");
        await WaitForServerAsync(DashboardBaseUrl, "Dashboard");

        // Initialize Playwright
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        Playwright?.Dispose();

        StopProcess(_apiProcess, "API");
        StopProcess(_dashboardProcess, "Dashboard");
    }

    public async Task<IPage> CreatePageAsync()
    {
        var context = await Browser.NewContextAsync();
        return await context.NewPageAsync();
    }

    private static Process StartProcess(string projectPath, int port, string name)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --launch-profile Local --urls http://localhost:{port}",
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Environment =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Local",
                ["ASPNETCORE_URLS"] = $"http://localhost:{port}"
            }
        };

        var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[{name}] {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[{name} ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private static void StopProcess(Process? process, string name)
    {
        if (process is null || process.HasExited)
            return;

        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
            Console.WriteLine($"[{name}] Process stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{name}] Error stopping process: {ex.Message}");
        }
    }

    private static async Task WaitForServerAsync(string baseUrl, string name, int timeoutSeconds = 60)
    {
        using var client = new HttpClient();
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
        {
            try
            {
                var response = await client.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"[{name}] Server ready at {baseUrl}");
                    return;
                }
            }
            catch
            {
                // Server not ready yet
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Server {name} at {baseUrl} did not start within {timeoutSeconds} seconds");
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindSolutionRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.slnx").Length > 0 ||
                Directory.GetFiles(dir, "*.sln").Length > 0)
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not find solution root directory");
    }
}

/// <summary>
/// Collection definition for tests that share the server fixture.
/// </summary>
[CollectionDefinition("UI Tests")]
public class UITestCollection : ICollectionFixture<TestServerFixture>
{
}

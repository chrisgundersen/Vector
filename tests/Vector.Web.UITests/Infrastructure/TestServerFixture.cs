using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;
using Xunit;

namespace Vector.Web.UITests.Infrastructure;

/// <summary>
/// Fixture that starts both the API and Underwriting Dashboard for E2E testing.
/// Uses actual process hosting for realistic testing.
/// Pre-builds projects to avoid compilation timeouts during server startup.
/// </summary>
public class TestServerFixture : IAsyncLifetime
{
    private Process? _apiProcess;
    private Process? _dashboardProcess;
    private string? _tempDbPath;

    public string ApiBaseUrl { get; private set; } = null!;
    public string DashboardBaseUrl { get; private set; } = null!;

    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    private static readonly string SolutionRoot = FindSolutionRoot();

    public async Task InitializeAsync()
    {
        // Create a temporary SQLite database file for test isolation
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"vector_uitest_{Guid.NewGuid():N}.db");

        // Find available ports
        var apiPort = GetAvailablePort();
        var dashboardPort = GetAvailablePort();

        ApiBaseUrl = $"http://localhost:{apiPort}";
        DashboardBaseUrl = $"http://localhost:{dashboardPort}";

        // Pre-build both projects to avoid compilation during startup
        Console.WriteLine("[Build] Building API and Dashboard projects...");
        await BuildProjectAsync(Path.Combine(SolutionRoot, "src", "Vector.Api"));
        await BuildProjectAsync(Path.Combine(SolutionRoot, "src", "Vector.Web.Underwriting"));
        Console.WriteLine("[Build] Build complete");

        // Start the API with --no-build (already compiled above)
        _apiProcess = StartProcess(
            Path.Combine(SolutionRoot, "src", "Vector.Api"),
            apiPort,
            "Vector.Api",
            seedDatabase: true);

        // Start the Dashboard with --no-build
        _dashboardProcess = StartProcess(
            Path.Combine(SolutionRoot, "src", "Vector.Web.Underwriting"),
            dashboardPort,
            "Vector.Web.Underwriting",
            seedDatabase: false);

        // Wait for both servers to be ready (with generous timeout)
        await Task.WhenAll(
            WaitForServerAsync(ApiBaseUrl, "API"),
            WaitForServerAsync(DashboardBaseUrl, "Dashboard"));

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

        // Clean up temporary database file
        if (_tempDbPath is not null && File.Exists(_tempDbPath))
        {
            try
            {
                File.Delete(_tempDbPath);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    public async Task<IPage> CreatePageAsync()
    {
        var context = await Browser.NewContextAsync();
        return await context.NewPageAsync();
    }

    private static async Task BuildProjectAsync(string projectPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build --configuration Debug --no-restore -v q",
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        // Capture output for debugging
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to build project at {projectPath}.\nOutput: {output}\nError: {error}");
        }
    }

    private Process StartProcess(string projectPath, int port, string name, bool seedDatabase)
    {
        // Use --no-build since we pre-built above. Don't use --launch-profile
        // to avoid URL conflicts; set all config via environment variables.
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --no-launch-profile",
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Environment =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Local",
                ["ASPNETCORE_URLS"] = $"http://localhost:{port}",
                ["ConnectionStrings__Sqlite"] = $"Data Source={_tempDbPath}",
                ["UseMockServices"] = "true",
                ["SeedDatabase"] = seedDatabase ? "true" : "false",
                ["Authentication__DisableAuthentication"] = "true"
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

    private static async Task WaitForServerAsync(string baseUrl, string name, int timeoutSeconds = 120)
    {
        using var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        });
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
        {
            try
            {
                var response = await client.GetAsync(baseUrl);
                // Accept any response as "server is up" - success, redirects, or not found
                var status = (int)response.StatusCode;
                if (status is >= 200 and < 500)
                {
                    Console.WriteLine($"[{name}] Server ready at {baseUrl} (status: {response.StatusCode})");
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

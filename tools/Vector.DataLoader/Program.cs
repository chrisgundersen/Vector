using System.CommandLine;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Spectre.Console;
using Vector.EndToEnd.Tests.TestData.Generators;

namespace Vector.DataLoader;

class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Vector Data Loader - Load realistic test data into a running Vector instance");

        var urlOption = new Option<string>(
            "--url",
            getDefaultValue: () => "http://localhost:5147",
            description: "The URL of the Vector API");

        var countOption = new Option<int>(
            "--count",
            getDefaultValue: () => 50,
            description: "Number of submissions to generate");

        var delayOption = new Option<int>(
            "--delay",
            getDefaultValue: () => 500,
            description: "Delay in milliseconds between submissions");

        var processOption = new Option<bool>(
            "--process",
            getDefaultValue: () => true,
            description: "Automatically process emails after sending");

        var seedOption = new Option<int?>(
            "--seed",
            description: "Random seed for reproducible data generation");

        rootCommand.AddOption(urlOption);
        rootCommand.AddOption(countOption);
        rootCommand.AddOption(delayOption);
        rootCommand.AddOption(processOption);
        rootCommand.AddOption(seedOption);

        rootCommand.SetHandler(async (url, count, delay, process, seed) =>
        {
            await RunDataLoader(url, count, delay, process, seed);
        }, urlOption, countOption, delayOption, processOption, seedOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunDataLoader(string baseUrl, int count, int delay, bool autoProcess, int? seed)
    {
        AnsiConsole.Write(new FigletText("Vector Data Loader").Color(Color.Blue));
        AnsiConsole.WriteLine();

        using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

        // Check if simulation is enabled
        AnsiConsole.Status()
            .Start("Checking simulation status...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
            });

        try
        {
            var statusResponse = await httpClient.GetAsync("/api/simulation/status");
            if (!statusResponse.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine("[red]Failed to connect to Vector API at {0}[/]", baseUrl);
                AnsiConsole.MarkupLine("[yellow]Make sure the API is running with 'dotnet run --launch-profile Local'[/]");
                return;
            }

            var status = await statusResponse.Content.ReadFromJsonAsync<SimulationStatus>(JsonOptions);
            if (status?.Enabled != true)
            {
                AnsiConsole.MarkupLine("[red]Simulation endpoints are not enabled[/]");
                AnsiConsole.MarkupLine("[yellow]Set EnableSimulationEndpoints=true in appsettings.Local.json[/]");
                return;
            }

            AnsiConsole.MarkupLine("[green]Connected to Vector API[/] - Simulation endpoints enabled");
            AnsiConsole.MarkupLine("Email Service: [cyan]{0}[/]", status.EmailServiceType);
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine("[red]Failed to connect to Vector API at {0}[/]", baseUrl);
            AnsiConsole.MarkupLine("[red]Error: {0}[/]", ex.Message);
            AnsiConsole.MarkupLine("[yellow]Make sure the API is running with:[/]");
            AnsiConsole.MarkupLine("  [cyan]cd src/Vector.Api && dotnet run --launch-profile Local[/]");
            return;
        }

        AnsiConsole.WriteLine();

        // Generate submissions
        var actualSeed = seed ?? Random.Shared.Next();
        AnsiConsole.MarkupLine("Generating [cyan]{0}[/] realistic submissions (seed: {1})...", count, actualSeed);

        var generator = new SubmissionGenerator(actualSeed);
        var sovGenerator = new SovGenerator(actualSeed);
        var submissions = generator.GenerateSubmissions(count);

        // Display scenario distribution
        var scenarioTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Scenario Type")
            .AddColumn("Count");

        foreach (var group in submissions.GroupBy(s => s.ScenarioType).OrderByDescending(g => g.Count()))
        {
            scenarioTable.AddRow(group.Key.ToString(), group.Count().ToString());
        }

        AnsiConsole.Write(scenarioTable);
        AnsiConsole.WriteLine();

        // Send submissions
        var sentCount = 0;
        var errorCount = 0;

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var sendTask = ctx.AddTask("[green]Sending submissions[/]", maxValue: count);

                foreach (var submission in submissions)
                {
                    try
                    {
                        // Generate SOV attachment
                        var sovBytes = sovGenerator.GenerateSovWorkbook(submission.Locations, submission.Insured);
                        var sovBase64 = Convert.ToBase64String(sovBytes);

                        // Create email request
                        var emailRequest = new
                        {
                            fromAddress = submission.Email.FromAddress,
                            fromName = submission.Email.FromName,
                            subject = submission.Email.Subject,
                            body = submission.Email.Body,
                            isHtml = false,
                            attachments = new[]
                            {
                                new
                                {
                                    fileName = $"{submission.Insured.Name.Replace(" ", "_")}_SOV.xlsx",
                                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    base64Content = sovBase64
                                },
                                new
                                {
                                    fileName = "ACORD125_Application.pdf",
                                    contentType = "application/pdf",
                                    base64Content = (string?)null
                                },
                                new
                                {
                                    fileName = "ACORD126_GL_Section.pdf",
                                    contentType = "application/pdf",
                                    base64Content = (string?)null
                                }
                            }
                        };

                        var response = await httpClient.PostAsJsonAsync("/api/simulation/emails", emailRequest, JsonOptions);

                        if (response.IsSuccessStatusCode)
                        {
                            sentCount++;
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                    catch
                    {
                        errorCount++;
                    }

                    sendTask.Increment(1);

                    if (delay > 0)
                    {
                        await Task.Delay(delay);
                    }
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Sent [green]{0}[/] submissions, [red]{1}[/] errors", sentCount, errorCount);

        // Process emails if requested
        if (autoProcess && sentCount > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Processing emails...[/]");

            var processResponse = await httpClient.PostAsync("/api/simulation/process-emails", null);
            if (processResponse.IsSuccessStatusCode)
            {
                var processResult = await processResponse.Content.ReadFromJsonAsync<ProcessResult>(JsonOptions);
                AnsiConsole.MarkupLine("Processed [green]{0}[/] of [cyan]{1}[/] emails",
                    processResult?.Processed ?? 0,
                    processResult?.TotalPending ?? 0);

                if (processResult?.Errors?.Count > 0)
                {
                    AnsiConsole.MarkupLine("[red]Errors:[/]");
                    foreach (var error in processResult.Errors.Take(5))
                    {
                        AnsiConsole.MarkupLine("  [red]- {0}[/]", error);
                    }
                }
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[green]Data Loading Complete[/]").RuleStyle("blue"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("View submissions in the Underwriting Dashboard:");
        AnsiConsole.MarkupLine("  [cyan]http://localhost:5183/submissions[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Or query the API:");
        AnsiConsole.MarkupLine("  [cyan]{0}/api/v1/submissions[/]", baseUrl);
    }

    record SimulationStatus(bool Enabled, string EmailServiceType, string Message);
    record ProcessResult(int TotalPending, int Processed, List<string>? Errors);
}

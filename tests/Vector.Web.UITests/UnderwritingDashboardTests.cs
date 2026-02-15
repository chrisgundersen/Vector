using FluentAssertions;
using Microsoft.Playwright;
using Vector.Web.UITests.Infrastructure;
using Xunit;

namespace Vector.Web.UITests;

/// <summary>
/// End-to-end UI tests for the Underwriting Dashboard.
/// </summary>
[Collection("UI Tests")]
public class UnderwritingDashboardTests(TestServerFixture fixture)
{
    private readonly TestServerFixture _fixture = fixture;

    [Fact]
    public async Task HomePage_ShouldLoad_WithDashboardTitle()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();

        // Act
        var response = await page.GotoAsync(_fixture.DashboardBaseUrl);

        // Assert
        response.Should().NotBeNull();
        response!.Status.Should().Be(200, "home page should return 200 OK");

        // Wait for page to fully load
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check page title
        var title = await page.TitleAsync();
        title.Should().Contain("Underwriting", "page title should contain 'Underwriting'");

        // Check that the dashboard heading is present
        var heading = page.Locator("h1");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        var headingText = await heading.TextContentAsync();
        headingText.Should().Contain("Dashboard", "main heading should contain 'Dashboard'");

        await page.CloseAsync();
    }

    [Fact]
    public async Task HomePage_ShouldDisplay_DashboardCards()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();

        // Act
        await page.GotoAsync(_fixture.DashboardBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - check for dashboard metric cards
        var cards = page.Locator(".card");
        var cardCount = await cards.CountAsync();
        cardCount.Should().BeGreaterThan(0, "dashboard should display metric cards");

        await page.CloseAsync();
    }

    [Fact]
    public async Task HomePage_ShouldNotRedirectToLogin_WhenAuthDisabled()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();

        // Act
        var response = await page.GotoAsync(_fixture.DashboardBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var currentUrl = page.Url;
        currentUrl.Should().NotContain("/Account/Login", "should not redirect to login when auth is disabled");
        currentUrl.Should().NotContain("/signin", "should not redirect to any sign-in page");

        await page.CloseAsync();
    }

    [Fact]
    public async Task NavigationMenu_ShouldBeVisible()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();

        // Act
        await page.GotoAsync(_fixture.DashboardBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - check for navigation elements
        var navMenu = page.Locator(".nav-link");
        var navCount = await navMenu.CountAsync();
        navCount.Should().BeGreaterThan(0, "navigation menu should have links");

        await page.CloseAsync();
    }

    [Fact]
    public async Task QueuePage_ShouldLoad_Successfully()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();

        // Act
        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/queue");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var currentUrl = page.Url;
        currentUrl.Should().Contain("/queue", "should navigate to queue page");

        // Page should not show error
        var errorBoundary = page.Locator(".blazor-error-boundary");
        var errorVisible = await errorBoundary.IsVisibleAsync();
        errorVisible.Should().BeFalse("queue page should not show error boundary");

        await page.CloseAsync();
    }

    [Fact]
    public async Task SubmissionsPage_ShouldLoad_Successfully()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();

        // Act
        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/submissions");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var currentUrl = page.Url;
        currentUrl.Should().Contain("/submissions", "should navigate to submissions page");

        // Page should not show error
        var errorBoundary = page.Locator(".blazor-error-boundary");
        var errorVisible = await errorBoundary.IsVisibleAsync();
        errorVisible.Should().BeFalse("submissions page should not show error boundary");

        await page.CloseAsync();
    }

    [Fact]
    public async Task MyWorkPage_ShouldLoad_Successfully()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();

        // Act
        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/my-work");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var currentUrl = page.Url;
        currentUrl.Should().Contain("/my-work", "should navigate to my-work page");

        // Page should not show error
        var errorBoundary = page.Locator(".blazor-error-boundary");
        var errorVisible = await errorBoundary.IsVisibleAsync();
        errorVisible.Should().BeFalse("my-work page should not show error boundary");

        await page.CloseAsync();
    }

    [Fact]
    public async Task Navigation_FromDashboard_ToQueue_ShouldWork()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();
        await page.GotoAsync(_fixture.DashboardBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - click on Queue link in navigation
        var queueLink = page.Locator("a[href='queue']").First;
        await queueLink.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var currentUrl = page.Url;
        currentUrl.Should().Contain("/queue", "clicking queue link should navigate to queue page");

        await page.CloseAsync();
    }

    [Fact]
    public async Task BlazorInteractivity_ShouldBeWorking()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();

        // Act
        await page.GotoAsync(_fixture.DashboardBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - check that Blazor JavaScript is loaded and working
        var blazorScript = page.Locator("script[src='_framework/blazor.web.js']");
        var scriptExists = await blazorScript.CountAsync() > 0;
        scriptExists.Should().BeTrue("Blazor script should be present");

        // Check that loading spinner is not stuck visible (would indicate Blazor connection issues)
        await Task.Delay(2000); // Give time for Blazor to initialize
        var spinner = page.Locator(".spinner-border");
        var spinnerVisible = await spinner.IsVisibleAsync();

        // Spinner might be briefly visible during data loading, but shouldn't persist
        if (spinnerVisible)
        {
            await Task.Delay(5000); // Wait a bit more
            spinnerVisible = await spinner.IsVisibleAsync();
            spinnerVisible.Should().BeFalse("loading spinner should not be stuck visible");
        }

        await page.CloseAsync();
    }

    [Fact]
    public async Task StaticAssets_ShouldLoad_Successfully()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();
        var failedRequests = new List<string>();

        page.RequestFailed += (_, request) =>
        {
            if (request.ResourceType == "stylesheet" || request.ResourceType == "script")
            {
                failedRequests.Add(request.Url);
            }
        };

        // Act
        await page.GotoAsync(_fixture.DashboardBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        failedRequests.Should().BeEmpty("all static assets should load successfully");

        await page.CloseAsync();
    }

    [Fact]
    public async Task NotFoundPage_ShouldDisplay_ForInvalidRoute()
    {
        // Arrange
        var page = await _fixture.CreatePageAsync();

        // Act
        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/this-page-does-not-exist");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - should show not found content, not a server error
        var pageContent = await page.ContentAsync();
        pageContent.Should().Contain("Not Found", "invalid route should show not found message");

        await page.CloseAsync();
    }
}

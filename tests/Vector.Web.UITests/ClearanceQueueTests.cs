using FluentAssertions;
using Microsoft.Playwright;
using Vector.Web.UITests.Infrastructure;
using Xunit;

namespace Vector.Web.UITests;

/// <summary>
/// End-to-end UI tests for the Clearance Queue page
/// and clearance-related UI elements.
/// </summary>
[Collection("UI Tests")]
public class ClearanceQueueTests(TestServerFixture fixture)
{
    private readonly TestServerFixture _fixture = fixture;

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Clearance")]
    public async Task ClearanceQueuePage_ShouldLoad_Successfully()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/clearance");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var currentUrl = page.Url;
        currentUrl.Should().Contain("/clearance", "should navigate to clearance page");

        var errorBoundary = page.Locator(".blazor-error-boundary");
        (await errorBoundary.IsVisibleAsync()).Should().BeFalse("clearance page should not show error boundary");

        await page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Clearance")]
    public async Task ClearanceQueuePage_ShouldHaveNavLink()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync(_fixture.DashboardBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var clearanceLink = page.Locator("a[href='clearance']");
        (await clearanceLink.CountAsync()).Should().BeGreaterThan(0,
            "Clearance Queue nav link should be visible");

        await page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Clearance")]
    public async Task Navigation_FromDashboard_ToClearanceQueue_ShouldWork()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync(_fixture.DashboardBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var clearanceLink = page.Locator("a[href='clearance']").First;
        await clearanceLink.ClickAsync();

        // Wait for Blazor client-side navigation
        var heading = page.Locator("h1:has-text('Clearance Queue')");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

        page.Url.Should().Contain("/clearance", "clicking clearance link should navigate to clearance page");

        await page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Clearance")]
    public async Task ClearanceQueuePage_ShouldDisplayHeading()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/clearance");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var heading = page.Locator("h1");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        var headingText = await heading.TextContentAsync();
        headingText.Should().Contain("Clearance Queue");

        await page.CloseAsync();
    }
}

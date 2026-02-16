using FluentAssertions;
using Microsoft.Playwright;
using Vector.Web.UITests.Infrastructure;
using Xunit;

namespace Vector.Web.UITests;

/// <summary>
/// End-to-end UI tests for the admin pages (Guidelines, Routing Rules, Pairings)
/// in the Underwriting Dashboard.
/// </summary>
[Collection("UI Tests")]
public class AdminPagesTests(TestServerFixture fixture)
{
    private readonly TestServerFixture _fixture = fixture;

    #region Navigation Menu Tests

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task AdminNavSection_ShouldBeVisible_ForAdminUser()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync(_fixture.DashboardBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The default mock user (John Smith) has Admin role, so admin nav links should be visible
        var guidelinesLink = page.Locator("a[href='admin/guidelines']");
        var routingLink = page.Locator("a[href='admin/routing']");
        var pairingsLink = page.Locator("a[href='admin/pairings']");

        (await guidelinesLink.CountAsync()).Should().BeGreaterThan(0, "Guidelines nav link should be visible for admin user");
        (await routingLink.CountAsync()).Should().BeGreaterThan(0, "Routing Rules nav link should be visible for admin user");
        (await pairingsLink.CountAsync()).Should().BeGreaterThan(0, "Pairings nav link should be visible for admin user");

        await page.CloseAsync();
    }

    #endregion

    #region Guidelines Page Tests

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task GuidelinesPage_ShouldLoad_Successfully()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/admin/guidelines");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify no Blazor error
        var errorBoundary = page.Locator(".blazor-error-boundary");
        (await errorBoundary.IsVisibleAsync()).Should().BeFalse("guidelines page should not show error boundary");

        // Verify heading is present
        var heading = page.Locator("h1");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        var headingText = await heading.TextContentAsync();
        headingText.Should().Contain("Underwriting Guidelines");

        await page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task GuidelinesPage_ShouldHave_CreateButton()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/admin/guidelines");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var createButton = page.Locator("a[href='/admin/guidelines/create']");
        (await createButton.CountAsync()).Should().BeGreaterThan(0, "should have a 'New Guideline' button");

        await page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task GuidelinesCreatePage_ShouldLoad_Successfully()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/admin/guidelines/create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var errorBoundary = page.Locator(".blazor-error-boundary");
        (await errorBoundary.IsVisibleAsync()).Should().BeFalse("guidelines create page should not show error boundary");

        // Verify form is present
        var form = page.Locator("form");
        (await form.CountAsync()).Should().BeGreaterThan(0, "create page should contain a form");

        // Verify breadcrumb navigates back to guidelines list
        var breadcrumbLink = page.Locator("a[href='/admin/guidelines']");
        (await breadcrumbLink.CountAsync()).Should().BeGreaterThan(0, "breadcrumb should link back to guidelines list");

        await page.CloseAsync();
    }

    #endregion

    #region Routing Rules Page Tests

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task RoutingRulesPage_ShouldLoad_Successfully()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/admin/routing");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var errorBoundary = page.Locator(".blazor-error-boundary");
        (await errorBoundary.IsVisibleAsync()).Should().BeFalse("routing rules page should not show error boundary");

        var heading = page.Locator("h1");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        var headingText = await heading.TextContentAsync();
        headingText.Should().Contain("Routing Rules");

        await page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task RoutingRulesPage_ShouldHave_CreateButton()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/admin/routing");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var createButton = page.Locator("a[href='/admin/routing/create']");
        (await createButton.CountAsync()).Should().BeGreaterThan(0, "should have a 'New Rule' button");

        await page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task RoutingRulesCreatePage_ShouldLoad_Successfully()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/admin/routing/create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var errorBoundary = page.Locator(".blazor-error-boundary");
        (await errorBoundary.IsVisibleAsync()).Should().BeFalse("routing rules create page should not show error boundary");

        var form = page.Locator("form");
        (await form.CountAsync()).Should().BeGreaterThan(0, "create page should contain a form");

        var breadcrumbLink = page.Locator("a[href='/admin/routing']");
        (await breadcrumbLink.CountAsync()).Should().BeGreaterThan(0, "breadcrumb should link back to routing rules list");

        await page.CloseAsync();
    }

    #endregion

    #region Pairings Page Tests

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task PairingsPage_ShouldLoad_Successfully()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/admin/pairings");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var errorBoundary = page.Locator(".blazor-error-boundary");
        (await errorBoundary.IsVisibleAsync()).Should().BeFalse("pairings page should not show error boundary");

        var heading = page.Locator("h1");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        var headingText = await heading.TextContentAsync();
        headingText.Should().Contain("Producer-Underwriter Pairings");

        await page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task PairingsPage_ShouldHave_CreateButton()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/admin/pairings");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var createButton = page.Locator("a[href='/admin/pairings/create']");
        (await createButton.CountAsync()).Should().BeGreaterThan(0, "should have a 'New Pairing' button");

        await page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task PairingsCreatePage_ShouldLoad_Successfully()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/admin/pairings/create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var errorBoundary = page.Locator(".blazor-error-boundary");
        (await errorBoundary.IsVisibleAsync()).Should().BeFalse("pairings create page should not show error boundary");

        var form = page.Locator("form");
        (await form.CountAsync()).Should().BeGreaterThan(0, "create page should contain a form");

        var breadcrumbLink = page.Locator("a[href='/admin/pairings']");
        (await breadcrumbLink.CountAsync()).Should().BeGreaterThan(0, "breadcrumb should link back to pairings list");

        await page.CloseAsync();
    }

    #endregion

    #region Cross-Page Navigation Tests

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task Navigation_FromDashboard_ToAdminGuidelines_ShouldWork()
    {
        var page = await _fixture.CreatePageAsync();

        await page.GotoAsync(_fixture.DashboardBaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click on Guidelines admin nav link
        var guidelinesLink = page.Locator("a[href='admin/guidelines']").First;
        await guidelinesLink.ClickAsync();

        // Wait for Blazor client-side navigation to complete by checking for the target heading text
        var heading = page.Locator("h1:has-text('Underwriting Guidelines')");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

        page.Url.Should().Contain("/admin/guidelines", "should navigate to guidelines page");

        await page.CloseAsync();
    }

    [Fact]
    [Trait("Category", "UITest")]
    [Trait("Category", "Admin")]
    public async Task Navigation_BetweenAdminPages_ShouldWork()
    {
        var page = await _fixture.CreatePageAsync();

        // Start at Guidelines
        await page.GotoAsync($"{_fixture.DashboardBaseUrl}/admin/guidelines");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate to Routing Rules via nav - wait for Blazor client-side navigation
        var routingLink = page.Locator("a[href='admin/routing']").First;
        await routingLink.ClickAsync();
        var routingHeading = page.Locator("h1:has-text('Routing Rules')");
        await routingHeading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

        page.Url.Should().Contain("/admin/routing");

        // Navigate to Pairings via nav - wait for Blazor client-side navigation
        var pairingsLink = page.Locator("a[href='admin/pairings']").First;
        await pairingsLink.ClickAsync();
        var pairingsHeading = page.Locator("h1:has-text('Producer-Underwriter Pairings')");
        await pairingsHeading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

        page.Url.Should().Contain("/admin/pairings");

        await page.CloseAsync();
    }

    #endregion
}

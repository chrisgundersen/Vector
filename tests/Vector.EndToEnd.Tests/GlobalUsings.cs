global using Xunit;
global using Microsoft.EntityFrameworkCore;

// Disable parallel test execution for E2E tests to avoid WebApplicationFactory conflicts
[assembly: CollectionBehavior(DisableTestParallelization = true)]

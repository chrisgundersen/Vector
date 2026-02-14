using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Entities;
using Vector.Domain.Routing.Enums;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Enums;
using Vector.Domain.Submission.ValueObjects;
using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Enums;

namespace Vector.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with sample data for development and testing.
/// </summary>
public class DatabaseSeeder(VectorDbContext context, ILogger<DatabaseSeeder> logger)
{
    // Well-known IDs for testing
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid SecondTenantId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid Underwriter1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Underwriter2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Producer1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid Producer2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting database seeding...");

        await SeedUnderwritingGuidelinesAsync(cancellationToken);
        await SeedRoutingRulesAsync(cancellationToken);
        await SeedProducerUnderwriterPairingsAsync(cancellationToken);
        await SeedSubmissionsAsync(cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Database seeding completed");
    }

    private async Task SeedUnderwritingGuidelinesAsync(CancellationToken cancellationToken)
    {
        if (await context.Set<UnderwritingGuideline>().AnyAsync(cancellationToken))
        {
            logger.LogInformation("Underwriting guidelines already seeded, skipping...");
            return;
        }

        logger.LogInformation("Seeding underwriting guidelines...");

        // General Liability guideline
        var glGuideline = UnderwritingGuideline.Create(DefaultTenantId, "General Liability - Standard");
        glGuideline.UpdateDetails(
            "General Liability - Standard",
            "Standard GL coverage for low-hazard commercial risks");
        glGuideline.SetApplicability("GeneralLiability", "NY,CA,TX,FL,IL", "44,45,52");
        glGuideline.AddRule("Min Premium Check", RuleType.Eligibility, RuleAction.Decline, 10);
        glGuideline.AddRule("Loss Ratio Check", RuleType.Appetite, RuleAction.Refer, 20);
        glGuideline.Activate();

        // Property guideline
        var propGuideline = UnderwritingGuideline.Create(DefaultTenantId, "Property - Manufacturing");
        propGuideline.UpdateDetails(
            "Property - Manufacturing",
            "Property coverage for manufacturing facilities");
        propGuideline.SetApplicability("Property", "NY,NJ,PA,CT,MA", "31,32,33");
        propGuideline.AddRule("Building Age Check", RuleType.Eligibility, RuleAction.Refer, 10);
        propGuideline.AddRule("TIV Limit Check", RuleType.Eligibility, RuleAction.Decline, 5);
        propGuideline.Activate();

        // Workers Comp guideline
        var wcGuideline = UnderwritingGuideline.Create(DefaultTenantId, "Workers Compensation - Office");
        wcGuideline.UpdateDetails(
            "Workers Compensation - Office",
            "WC coverage for office-based occupational classes");
        wcGuideline.SetApplicability("WorkersCompensation", "US", "51,52,54,55,56");
        wcGuideline.AddRule("Experience Mod Check", RuleType.Pricing, RuleAction.ApplyModifier, 15);
        wcGuideline.Activate();

        // Second tenant guideline
        var cyberGuideline = UnderwritingGuideline.Create(SecondTenantId, "Cyber Liability - Technology");
        cyberGuideline.UpdateDetails(
            "Cyber Liability - Technology",
            "Cyber coverage for technology companies");
        cyberGuideline.SetApplicability("CyberLiability", "US,CA,UK", "51,52,54");
        cyberGuideline.AddRule("Security Assessment", RuleType.Eligibility, RuleAction.Refer, 10);
        cyberGuideline.Activate();

        context.Set<UnderwritingGuideline>().AddRange([glGuideline, propGuideline, wcGuideline, cyberGuideline]);
    }

    private async Task SeedRoutingRulesAsync(CancellationToken cancellationToken)
    {
        if (await context.Set<RoutingRule>().AnyAsync(cancellationToken))
        {
            logger.LogInformation("Routing rules already seeded, skipping...");
            return;
        }

        logger.LogInformation("Seeding routing rules...");

        // High value manufacturing - direct to senior UW
        var rule1Result = RoutingRule.Create(
            "High Value Manufacturing",
            "Route high-value manufacturing risks to senior underwriter",
            RoutingStrategy.Direct);
        var rule1 = rule1Result.Value;
        rule1.SetPriority(100);
        rule1.SetTargetUnderwriter(Underwriter1Id, "John Smith - Senior UW");
        rule1.Activate();

        // Standard commercial - round robin
        var rule2Result = RoutingRule.Create(
            "Standard Commercial",
            "Route standard commercial risks using round robin",
            RoutingStrategy.RoundRobin);
        var rule2 = rule2Result.Value;
        rule2.SetPriority(50);
        rule2.Activate();

        // Manual queue for complex risks
        var rule3Result = RoutingRule.Create(
            "Complex Risks",
            "Route complex risks to manual queue for assignment",
            RoutingStrategy.ManualQueue);
        var rule3 = rule3Result.Value;
        rule3.SetPriority(25);
        rule3.Activate();

        context.Set<RoutingRule>().AddRange([rule1, rule2, rule3]);
    }

    private async Task SeedProducerUnderwriterPairingsAsync(CancellationToken cancellationToken)
    {
        if (await context.Set<ProducerUnderwriterPairing>().AnyAsync(cancellationToken))
        {
            logger.LogInformation("Producer-underwriter pairings already seeded, skipping...");
            return;
        }

        logger.LogInformation("Seeding producer-underwriter pairings...");

        var pairing1 = ProducerUnderwriterPairing.Create(
            Producer1Id, "ABC Insurance Agency",
            Underwriter1Id, "John Smith");

        var pairing2 = ProducerUnderwriterPairing.Create(
            Producer2Id, "XYZ Brokers Inc",
            Underwriter2Id, "Jane Doe");

        context.Set<ProducerUnderwriterPairing>().AddRange([pairing1, pairing2]);
    }

    private async Task SeedSubmissionsAsync(CancellationToken cancellationToken)
    {
        if (await context.Set<Submission>().AnyAsync(cancellationToken))
        {
            logger.LogInformation("Submissions already seeded, skipping...");
            return;
        }

        logger.LogInformation("Seeding sample submissions...");

        var submissions = new List<Submission>();

        // 1. Draft submission
        var draft = CreateSubmission(DefaultTenantId, "SUB-2024-000001", "Acme Manufacturing Inc");
        submissions.Add(draft);

        // 2. Received submission
        var received = CreateSubmission(DefaultTenantId, "SUB-2024-000002", "Beta Retail Corp");
        received.MarkAsReceived();
        submissions.Add(received);

        // 3. In Review submission (assigned to underwriter)
        var inReview = CreateSubmission(DefaultTenantId, "SUB-2024-000003", "Gamma Technologies LLC");
        inReview.MarkAsReceived();
        inReview.AssignToUnderwriter(Underwriter1Id, "John Smith");
        AddSampleCoverages(inReview);
        AddSampleLocations(inReview);
        submissions.Add(inReview);

        // 4. Pending Information submission
        var pendingInfo = CreateSubmission(DefaultTenantId, "SUB-2024-000004", "Delta Construction Co");
        pendingInfo.MarkAsReceived();
        pendingInfo.AssignToUnderwriter(Underwriter2Id, "Jane Doe");
        pendingInfo.RequestInformation("Please provide 5-year loss history and current SOV.");
        submissions.Add(pendingInfo);

        // 5. Quoted submission
        var quoted = CreateSubmission(DefaultTenantId, "SUB-2024-000005", "Epsilon Logistics Inc");
        quoted.MarkAsReceived();
        quoted.AssignToUnderwriter(Underwriter1Id, "John Smith");
        AddSampleCoverages(quoted);
        AddSampleLocations(quoted);
        quoted.Quote(Money.FromDecimal(45000, "USD"));
        submissions.Add(quoted);

        // 6. Another quoted submission ready for binding
        var readyToBind = CreateSubmission(DefaultTenantId, "SUB-2024-000006", "Zeta Healthcare Systems");
        readyToBind.MarkAsReceived();
        readyToBind.AssignToUnderwriter(Underwriter1Id, "John Smith");
        AddSampleCoverages(readyToBind);
        AddSampleLocations(readyToBind);
        readyToBind.Quote(Money.FromDecimal(125000, "USD"));
        submissions.Add(readyToBind);

        // 7. Bound submission
        var bound = CreateSubmission(DefaultTenantId, "SUB-2024-000007", "Eta Financial Services");
        bound.MarkAsReceived();
        bound.AssignToUnderwriter(Underwriter2Id, "Jane Doe");
        AddSampleCoverages(bound);
        bound.Quote(Money.FromDecimal(75000, "USD"));
        bound.Bind();
        submissions.Add(bound);

        // 8. Declined submission
        var declined = CreateSubmission(DefaultTenantId, "SUB-2024-000008", "Theta Mining Corp");
        declined.MarkAsReceived();
        declined.AssignToUnderwriter(Underwriter1Id, "John Smith");
        declined.Decline("Outside appetite - mining operations not covered.");
        submissions.Add(declined);

        // 9. Second tenant submission - in review
        var tenant2Sub1 = CreateSubmission(SecondTenantId, "SUB-2024-000009", "Iota Software Inc");
        tenant2Sub1.MarkAsReceived();
        tenant2Sub1.AssignToUnderwriter(Underwriter1Id, "John Smith");
        AddSampleCoverages(tenant2Sub1);
        submissions.Add(tenant2Sub1);

        // 10. Second tenant submission - received
        var tenant2Sub2 = CreateSubmission(SecondTenantId, "SUB-2024-000010", "Kappa Cloud Services");
        tenant2Sub2.MarkAsReceived();
        submissions.Add(tenant2Sub2);

        foreach (var submission in submissions)
        {
            context.Set<Submission>().Add(submission);
        }
    }

    private static Submission CreateSubmission(Guid tenantId, string submissionNumber, string insuredName)
    {
        var result = Submission.Create(tenantId, submissionNumber, insuredName);
        return result.Value;
    }

    private static void AddSampleCoverages(Submission submission)
    {
        var glCoverage = submission.AddCoverage(CoverageType.GeneralLiability);
        glCoverage.UpdateRequestedLimit(Money.FromDecimal(1_000_000, "USD"));
        glCoverage.UpdateRequestedDeductible(Money.FromDecimal(10_000, "USD"));

        var propCoverage = submission.AddCoverage(CoverageType.PropertyDamage);
        propCoverage.UpdateRequestedLimit(Money.FromDecimal(5_000_000, "USD"));
        propCoverage.UpdateRequestedDeductible(Money.FromDecimal(25_000, "USD"));
    }

    private static void AddSampleLocations(Submission submission)
    {
        var address1 = Address.Create(
            "123 Main Street",
            null,
            "New York",
            "NY",
            "10001",
            "US");
        var location1 = submission.AddLocation(address1.Value);
        location1.UpdateOccupancyType("Manufacturing Facility");
        location1.UpdateSquareFootage(50000);
        location1.UpdateConstruction(null, 1985, null);

        var address2 = Address.Create(
            "456 Industrial Blvd",
            null,
            "Chicago",
            "IL",
            "60601",
            "US");
        var location2 = submission.AddLocation(address2.Value);
        location2.UpdateOccupancyType("Warehouse");
        location2.UpdateSquareFootage(75000);
        location2.UpdateConstruction(null, 2005, null);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Entities;
using Vector.Domain.Routing.Enums;
using Vector.Domain.Submission.Aggregates;
using Vector.Domain.Submission.Entities;
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

    // Underwriters
    public static readonly Guid Underwriter1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Underwriter2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Underwriter3Id = Guid.Parse("11111111-1111-1111-1111-111111111112");

    // Producers (Brokers/Agencies)
    public static readonly Guid Producer1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid Producer2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid Producer3Id = Guid.Parse("33333333-3333-3333-3333-333333333334");
    public static readonly Guid Producer4Id = Guid.Parse("33333333-3333-3333-3333-333333333335");

    // Producer names for reference
    private static readonly (Guid Id, string Name, string Email)[] Producers =
    [
        (Producer1Id, "ABC Insurance Agency", "submissions@abcinsurance.com"),
        (Producer2Id, "XYZ Brokers Inc", "newbusiness@xyzbrokers.com"),
        (Producer3Id, "Marsh McLennan", "submissions@marsh.com"),
        (Producer4Id, "Aon Risk Solutions", "newbusiness@aon.com")
    ];

    // Underwriter names for reference
    private static readonly (Guid Id, string Name)[] Underwriters =
    [
        (Underwriter1Id, "John Smith"),
        (Underwriter2Id, "Jane Doe"),
        (Underwriter3Id, "Mike Johnson")
    ];

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

        // ABC Insurance Agency -> John Smith (Senior UW)
        var pairing1 = ProducerUnderwriterPairing.Create(
            Producer1Id, "ABC Insurance Agency",
            Underwriter1Id, "John Smith");

        // XYZ Brokers Inc -> Jane Doe
        var pairing2 = ProducerUnderwriterPairing.Create(
            Producer2Id, "XYZ Brokers Inc",
            Underwriter2Id, "Jane Doe");

        // Marsh McLennan -> John Smith (handles large accounts)
        var pairing3 = ProducerUnderwriterPairing.Create(
            Producer3Id, "Marsh McLennan",
            Underwriter1Id, "John Smith");

        // Aon Risk Solutions -> Mike Johnson
        var pairing4 = ProducerUnderwriterPairing.Create(
            Producer4Id, "Aon Risk Solutions",
            Underwriter3Id, "Mike Johnson");

        context.Set<ProducerUnderwriterPairing>().AddRange([pairing1, pairing2, pairing3, pairing4]);
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
        var random = new Random(42); // Fixed seed for reproducibility

        // 1. Draft submission - ABC Insurance Agency
        var draft = CreateSubmission(DefaultTenantId, "SUB-2024-000001", "Acme Manufacturing Inc");
        draft.UpdateProducerInfo(Producer1Id, "ABC Insurance Agency", "submissions@abcinsurance.com");
        submissions.Add(draft);

        // 2. Received submission - XYZ Brokers
        var received = CreateSubmission(DefaultTenantId, "SUB-2024-000002", "Beta Retail Corp");
        received.UpdateProducerInfo(Producer2Id, "XYZ Brokers Inc", "newbusiness@xyzbrokers.com");
        received.MarkAsReceived();
        AddRetailCoverages(received);
        AddRetailLocations(received);
        received.UpdateScores(72, 65, 85);
        submissions.Add(received);

        // 3. In Review - Manufacturing risk with good loss history
        var inReview = CreateSubmission(DefaultTenantId, "SUB-2024-000003", "Gamma Technologies LLC");
        inReview.UpdateProducerInfo(Producer1Id, "ABC Insurance Agency", "submissions@abcinsurance.com");
        inReview.MarkAsReceived();
        inReview.AssignToUnderwriter(Underwriter1Id, "John Smith");
        AddManufacturingCoverages(inReview);
        AddManufacturingLocations(inReview);
        AddCleanLossHistory(inReview);
        inReview.UpdateScores(88, 75, 92);
        submissions.Add(inReview);

        // 4. Pending Information - Construction risk
        var pendingInfo = CreateSubmission(DefaultTenantId, "SUB-2024-000004", "Delta Construction Co");
        pendingInfo.UpdateProducerInfo(Producer2Id, "XYZ Brokers Inc", "newbusiness@xyzbrokers.com");
        pendingInfo.MarkAsReceived();
        pendingInfo.AssignToUnderwriter(Underwriter2Id, "Jane Doe");
        AddConstructionCoverages(pendingInfo);
        pendingInfo.UpdateScores(65, 58, 45);
        pendingInfo.RequestInformation("Please provide 5-year loss history and current SOV.");
        submissions.Add(pendingInfo);

        // 5. Quoted - Logistics with moderate loss history
        var quoted = CreateSubmission(DefaultTenantId, "SUB-2024-000005", "Epsilon Logistics Inc");
        quoted.UpdateProducerInfo(Producer3Id, "Marsh McLennan", "submissions@marsh.com");
        quoted.MarkAsReceived();
        quoted.AssignToUnderwriter(Underwriter1Id, "John Smith");
        AddLogisticsCoverages(quoted);
        AddWarehouseLocations(quoted);
        AddModerateLossHistory(quoted);
        quoted.UpdateScores(78, 82, 88);
        quoted.Quote(Money.FromDecimal(45000, "USD"));
        submissions.Add(quoted);

        // 6. Quoted ready for binding - Healthcare with excellent profile
        var readyToBind = CreateSubmission(DefaultTenantId, "SUB-2024-000006", "Zeta Healthcare Systems");
        readyToBind.UpdateProducerInfo(Producer4Id, "Aon Risk Solutions", "newbusiness@aon.com");
        readyToBind.MarkAsReceived();
        readyToBind.AssignToUnderwriter(Underwriter3Id, "Mike Johnson");
        AddHealthcareCoverages(readyToBind);
        AddHealthcareLocations(readyToBind);
        AddCleanLossHistory(readyToBind);
        readyToBind.UpdateScores(92, 88, 95);
        readyToBind.Quote(Money.FromDecimal(125000, "USD"));
        submissions.Add(readyToBind);

        // 7. Bound - Financial services
        var bound = CreateSubmission(DefaultTenantId, "SUB-2024-000007", "Eta Financial Services");
        bound.UpdateProducerInfo(Producer2Id, "XYZ Brokers Inc", "newbusiness@xyzbrokers.com");
        bound.MarkAsReceived();
        bound.AssignToUnderwriter(Underwriter2Id, "Jane Doe");
        AddProfessionalServicesCoverages(bound);
        AddOfficeLocations(bound);
        AddCleanLossHistory(bound);
        bound.UpdateScores(85, 90, 92);
        bound.Quote(Money.FromDecimal(75000, "USD"));
        bound.Bind();
        submissions.Add(bound);

        // 8. Declined - Mining (outside appetite)
        var declined = CreateSubmission(DefaultTenantId, "SUB-2024-000008", "Theta Mining Corp");
        declined.UpdateProducerInfo(Producer1Id, "ABC Insurance Agency", "submissions@abcinsurance.com");
        declined.MarkAsReceived();
        declined.AssignToUnderwriter(Underwriter1Id, "John Smith");
        AddHighRiskCoverages(declined);
        AddHeavyLossHistory(declined);
        declined.UpdateScores(25, 30, 70);
        declined.Decline("Outside appetite - mining operations not covered.");
        submissions.Add(declined);

        // 9. In Review - Tech company (second tenant)
        var tenant2Sub1 = CreateSubmission(SecondTenantId, "SUB-2024-000009", "Iota Software Inc");
        tenant2Sub1.UpdateProducerInfo(Producer3Id, "Marsh McLennan", "submissions@marsh.com");
        tenant2Sub1.MarkAsReceived();
        tenant2Sub1.AssignToUnderwriter(Underwriter1Id, "John Smith");
        AddTechCoverages(tenant2Sub1);
        AddOfficeLocations(tenant2Sub1);
        tenant2Sub1.UpdateScores(82, 78, 90);
        submissions.Add(tenant2Sub1);

        // 10. Received - Cloud services (second tenant)
        var tenant2Sub2 = CreateSubmission(SecondTenantId, "SUB-2024-000010", "Kappa Cloud Services");
        tenant2Sub2.UpdateProducerInfo(Producer4Id, "Aon Risk Solutions", "newbusiness@aon.com");
        tenant2Sub2.MarkAsReceived();
        AddTechCoverages(tenant2Sub2);
        AddDataCenterLocations(tenant2Sub2);
        tenant2Sub2.UpdateScores(75, 70, 82);
        submissions.Add(tenant2Sub2);

        foreach (var submission in submissions)
        {
            context.Set<Submission>().Add(submission);
        }
    }

    private static Submission CreateSubmission(Guid tenantId, string submissionNumber, string insuredName)
    {
        var result = Submission.Create(tenantId, submissionNumber, insuredName);
        var submission = result.Value;
        submission.UpdatePolicyDates(
            DateTime.UtcNow.AddMonths(1).Date,
            DateTime.UtcNow.AddMonths(13).Date);
        return submission;
    }

    #region Coverage Templates

    private static void AddManufacturingCoverages(Submission submission)
    {
        var gl = submission.AddCoverage(CoverageType.GeneralLiability);
        gl.UpdateRequestedLimit(Money.FromDecimal(2_000_000, "USD"));
        gl.UpdateRequestedDeductible(Money.FromDecimal(25_000, "USD"));

        var prop = submission.AddCoverage(CoverageType.PropertyDamage);
        prop.UpdateRequestedLimit(Money.FromDecimal(10_000_000, "USD"));
        prop.UpdateRequestedDeductible(Money.FromDecimal(50_000, "USD"));

        var products = submission.AddCoverage(CoverageType.ProductsCompleted);
        products.UpdateRequestedLimit(Money.FromDecimal(2_000_000, "USD"));
        products.UpdateRequestedDeductible(Money.FromDecimal(25_000, "USD"));
    }

    private static void AddRetailCoverages(Submission submission)
    {
        var gl = submission.AddCoverage(CoverageType.GeneralLiability);
        gl.UpdateRequestedLimit(Money.FromDecimal(1_000_000, "USD"));
        gl.UpdateRequestedDeductible(Money.FromDecimal(10_000, "USD"));

        var prop = submission.AddCoverage(CoverageType.PropertyDamage);
        prop.UpdateRequestedLimit(Money.FromDecimal(3_000_000, "USD"));
        prop.UpdateRequestedDeductible(Money.FromDecimal(25_000, "USD"));
    }

    private static void AddConstructionCoverages(Submission submission)
    {
        var gl = submission.AddCoverage(CoverageType.GeneralLiability);
        gl.UpdateRequestedLimit(Money.FromDecimal(5_000_000, "USD"));
        gl.UpdateRequestedDeductible(Money.FromDecimal(50_000, "USD"));

        var wc = submission.AddCoverage(CoverageType.WorkersCompensation);
        wc.UpdateRequestedLimit(Money.FromDecimal(1_000_000, "USD"));
    }

    private static void AddLogisticsCoverages(Submission submission)
    {
        var gl = submission.AddCoverage(CoverageType.GeneralLiability);
        gl.UpdateRequestedLimit(Money.FromDecimal(2_000_000, "USD"));
        gl.UpdateRequestedDeductible(Money.FromDecimal(25_000, "USD"));

        var prop = submission.AddCoverage(CoverageType.PropertyDamage);
        prop.UpdateRequestedLimit(Money.FromDecimal(8_000_000, "USD"));
        prop.UpdateRequestedDeductible(Money.FromDecimal(50_000, "USD"));

        var auto = submission.AddCoverage(CoverageType.Auto);
        auto.UpdateRequestedLimit(Money.FromDecimal(1_000_000, "USD"));
        auto.UpdateRequestedDeductible(Money.FromDecimal(5_000, "USD"));
    }

    private static void AddHealthcareCoverages(Submission submission)
    {
        var gl = submission.AddCoverage(CoverageType.GeneralLiability);
        gl.UpdateRequestedLimit(Money.FromDecimal(3_000_000, "USD"));
        gl.UpdateRequestedDeductible(Money.FromDecimal(50_000, "USD"));

        var prof = submission.AddCoverage(CoverageType.ProfessionalLiability);
        prof.UpdateRequestedLimit(Money.FromDecimal(5_000_000, "USD"));
        prof.UpdateRequestedDeductible(Money.FromDecimal(100_000, "USD"));

        var prop = submission.AddCoverage(CoverageType.PropertyDamage);
        prop.UpdateRequestedLimit(Money.FromDecimal(15_000_000, "USD"));
        prop.UpdateRequestedDeductible(Money.FromDecimal(100_000, "USD"));
    }

    private static void AddProfessionalServicesCoverages(Submission submission)
    {
        var gl = submission.AddCoverage(CoverageType.GeneralLiability);
        gl.UpdateRequestedLimit(Money.FromDecimal(1_000_000, "USD"));
        gl.UpdateRequestedDeductible(Money.FromDecimal(10_000, "USD"));

        var prof = submission.AddCoverage(CoverageType.ProfessionalLiability);
        prof.UpdateRequestedLimit(Money.FromDecimal(3_000_000, "USD"));
        prof.UpdateRequestedDeductible(Money.FromDecimal(50_000, "USD"));

        var cyber = submission.AddCoverage(CoverageType.Cyber);
        cyber.UpdateRequestedLimit(Money.FromDecimal(2_000_000, "USD"));
        cyber.UpdateRequestedDeductible(Money.FromDecimal(25_000, "USD"));
    }

    private static void AddTechCoverages(Submission submission)
    {
        var gl = submission.AddCoverage(CoverageType.GeneralLiability);
        gl.UpdateRequestedLimit(Money.FromDecimal(2_000_000, "USD"));
        gl.UpdateRequestedDeductible(Money.FromDecimal(25_000, "USD"));

        var prof = submission.AddCoverage(CoverageType.ProfessionalLiability);
        prof.UpdateRequestedLimit(Money.FromDecimal(5_000_000, "USD"));
        prof.UpdateRequestedDeductible(Money.FromDecimal(100_000, "USD"));

        var cyber = submission.AddCoverage(CoverageType.Cyber);
        cyber.UpdateRequestedLimit(Money.FromDecimal(10_000_000, "USD"));
        cyber.UpdateRequestedDeductible(Money.FromDecimal(100_000, "USD"));
    }

    private static void AddHighRiskCoverages(Submission submission)
    {
        var gl = submission.AddCoverage(CoverageType.GeneralLiability);
        gl.UpdateRequestedLimit(Money.FromDecimal(10_000_000, "USD"));
        gl.UpdateRequestedDeductible(Money.FromDecimal(100_000, "USD"));

        var prop = submission.AddCoverage(CoverageType.PropertyDamage);
        prop.UpdateRequestedLimit(Money.FromDecimal(50_000_000, "USD"));
        prop.UpdateRequestedDeductible(Money.FromDecimal(250_000, "USD"));

        var wc = submission.AddCoverage(CoverageType.WorkersCompensation);
        wc.UpdateRequestedLimit(Money.FromDecimal(2_000_000, "USD"));
    }

    #endregion

    #region Location Templates

    private static void AddManufacturingLocations(Submission submission)
    {
        var addr1 = Address.Create("123 Industrial Parkway", null, "Detroit", "MI", "48201", "US");
        var loc1 = submission.AddLocation(addr1.Value);
        loc1.UpdateOccupancyType("Manufacturing Facility");
        loc1.UpdateBuildingDescription("Main manufacturing plant with assembly lines");
        loc1.UpdateSquareFootage(150000);
        loc1.UpdateConstruction("Fire Resistive", 1995, 2);
        loc1.UpdateValues(
            Money.FromDecimal(8_000_000, "USD"),
            Money.FromDecimal(3_000_000, "USD"),
            Money.FromDecimal(1_500_000, "USD"));
        loc1.UpdateProtection(true, true, true, "2");

        var addr2 = Address.Create("456 Commerce Drive", null, "Toledo", "OH", "43604", "US");
        var loc2 = submission.AddLocation(addr2.Value);
        loc2.UpdateOccupancyType("Warehouse");
        loc2.UpdateBuildingDescription("Raw materials and finished goods storage");
        loc2.UpdateSquareFootage(75000);
        loc2.UpdateConstruction("Non-Combustible", 2005, 1);
        loc2.UpdateValues(
            Money.FromDecimal(3_000_000, "USD"),
            Money.FromDecimal(5_000_000, "USD"),
            Money.FromDecimal(500_000, "USD"));
        loc2.UpdateProtection(true, true, false, "3");
    }

    private static void AddRetailLocations(Submission submission)
    {
        var addr1 = Address.Create("789 Main Street", null, "Dallas", "TX", "75201", "US");
        var loc1 = submission.AddLocation(addr1.Value);
        loc1.UpdateOccupancyType("Retail Store");
        loc1.UpdateBuildingDescription("Flagship retail location in downtown");
        loc1.UpdateSquareFootage(25000);
        loc1.UpdateConstruction("Fire Resistive", 2010, 1);
        loc1.UpdateValues(
            Money.FromDecimal(2_000_000, "USD"),
            Money.FromDecimal(1_500_000, "USD"),
            Money.FromDecimal(800_000, "USD"));
        loc1.UpdateProtection(true, true, true, "2");
    }

    private static void AddWarehouseLocations(Submission submission)
    {
        var addr1 = Address.Create("100 Logistics Way", null, "Memphis", "TN", "38118", "US");
        var loc1 = submission.AddLocation(addr1.Value);
        loc1.UpdateOccupancyType("Distribution Center");
        loc1.UpdateBuildingDescription("Regional distribution hub");
        loc1.UpdateSquareFootage(200000);
        loc1.UpdateConstruction("Non-Combustible", 2015, 1);
        loc1.UpdateValues(
            Money.FromDecimal(5_000_000, "USD"),
            Money.FromDecimal(8_000_000, "USD"),
            Money.FromDecimal(2_000_000, "USD"));
        loc1.UpdateProtection(true, true, true, "2");

        var addr2 = Address.Create("200 Freight Blvd", null, "Louisville", "KY", "40202", "US");
        var loc2 = submission.AddLocation(addr2.Value);
        loc2.UpdateOccupancyType("Cross-Dock Facility");
        loc2.UpdateSquareFootage(80000);
        loc2.UpdateConstruction("Non-Combustible", 2018, 1);
        loc2.UpdateValues(
            Money.FromDecimal(2_500_000, "USD"),
            Money.FromDecimal(1_000_000, "USD"),
            Money.FromDecimal(500_000, "USD"));
        loc2.UpdateProtection(true, true, false, "3");
    }

    private static void AddHealthcareLocations(Submission submission)
    {
        var addr1 = Address.Create("500 Medical Center Dr", null, "Houston", "TX", "77030", "US");
        var loc1 = submission.AddLocation(addr1.Value);
        loc1.UpdateOccupancyType("Hospital");
        loc1.UpdateBuildingDescription("Main hospital campus - 300 beds");
        loc1.UpdateSquareFootage(400000);
        loc1.UpdateConstruction("Fire Resistive", 2000, 8);
        loc1.UpdateValues(
            Money.FromDecimal(75_000_000, "USD"),
            Money.FromDecimal(25_000_000, "USD"),
            Money.FromDecimal(15_000_000, "USD"));
        loc1.UpdateProtection(true, true, true, "1");

        var addr2 = Address.Create("510 Clinic Way", null, "Houston", "TX", "77031", "US");
        var loc2 = submission.AddLocation(addr2.Value);
        loc2.UpdateOccupancyType("Medical Office Building");
        loc2.UpdateSquareFootage(50000);
        loc2.UpdateConstruction("Fire Resistive", 2012, 3);
        loc2.UpdateValues(
            Money.FromDecimal(8_000_000, "USD"),
            Money.FromDecimal(2_000_000, "USD"),
            Money.FromDecimal(1_000_000, "USD"));
        loc2.UpdateProtection(true, true, true, "2");
    }

    private static void AddOfficeLocations(Submission submission)
    {
        var addr1 = Address.Create("One Financial Plaza", "Suite 2500", "New York", "NY", "10004", "US");
        var loc1 = submission.AddLocation(addr1.Value);
        loc1.UpdateOccupancyType("Office - Professional Services");
        loc1.UpdateBuildingDescription("Corporate headquarters");
        loc1.UpdateSquareFootage(35000);
        loc1.UpdateConstruction("Fire Resistive", 2008, 25);
        loc1.UpdateValues(
            Money.FromDecimal(0, "USD"), // Leased space
            Money.FromDecimal(2_000_000, "USD"),
            Money.FromDecimal(3_000_000, "USD"));
        loc1.UpdateProtection(true, true, true, "1");
    }

    private static void AddDataCenterLocations(Submission submission)
    {
        var addr1 = Address.Create("1000 Cloud Drive", null, "Ashburn", "VA", "20147", "US");
        var loc1 = submission.AddLocation(addr1.Value);
        loc1.UpdateOccupancyType("Data Center");
        loc1.UpdateBuildingDescription("Tier 4 data center facility");
        loc1.UpdateSquareFootage(100000);
        loc1.UpdateConstruction("Fire Resistive", 2019, 2);
        loc1.UpdateValues(
            Money.FromDecimal(25_000_000, "USD"),
            Money.FromDecimal(50_000_000, "USD"),
            Money.FromDecimal(20_000_000, "USD"));
        loc1.UpdateProtection(true, true, true, "1");
    }

    #endregion

    #region Loss History Templates

    private static void AddCleanLossHistory(Submission submission)
    {
        // One small claim from 3 years ago, closed
        var loss = submission.AddLoss(DateTime.UtcNow.AddYears(-3), "Minor slip and fall incident in lobby");
        loss.UpdateClaimInfo("CLM-2021-0042", CoverageType.GeneralLiability, "Prior Carrier Insurance Co");
        loss.UpdateAmounts(
            Money.FromDecimal(8_500, "USD"),
            Money.FromDecimal(0, "USD"),
            null);
        loss.UpdateStatus(LossStatus.ClosedWithPayment);
    }

    private static void AddModerateLossHistory(Submission submission)
    {
        // Claim 1 - Property damage 2 years ago
        var loss1 = submission.AddLoss(DateTime.UtcNow.AddYears(-2), "Roof leak caused water damage to inventory");
        loss1.UpdateClaimInfo("CLM-2022-0156", CoverageType.PropertyDamage, "National Insurance Co");
        loss1.UpdateAmounts(
            Money.FromDecimal(45_000, "USD"),
            Money.FromDecimal(0, "USD"),
            null);
        loss1.UpdateStatus(LossStatus.ClosedWithPayment);

        // Claim 2 - Auto accident 18 months ago
        var loss2 = submission.AddLoss(DateTime.UtcNow.AddMonths(-18), "Delivery truck rear-ended at stoplight");
        loss2.UpdateClaimInfo("CLM-2023-0089", CoverageType.Auto, "National Insurance Co");
        loss2.UpdateAmounts(
            Money.FromDecimal(22_000, "USD"),
            Money.FromDecimal(0, "USD"),
            null);
        loss2.UpdateStatus(LossStatus.ClosedWithPayment);

        // Claim 3 - Minor GL 6 months ago (still open)
        var loss3 = submission.AddLoss(DateTime.UtcNow.AddMonths(-6), "Customer injured by falling merchandise");
        loss3.UpdateClaimInfo("CLM-2024-0234", CoverageType.GeneralLiability, "National Insurance Co");
        loss3.UpdateAmounts(
            Money.FromDecimal(5_000, "USD"),
            Money.FromDecimal(15_000, "USD"),
            null);
        loss3.UpdateStatus(LossStatus.Open);
    }

    private static void AddHeavyLossHistory(Submission submission)
    {
        // Multiple significant claims indicating poor risk
        var loss1 = submission.AddLoss(DateTime.UtcNow.AddYears(-2), "Equipment malfunction caused employee injury");
        loss1.UpdateClaimInfo("CLM-2022-0892", CoverageType.WorkersCompensation, "Workers Comp Carrier");
        loss1.UpdateAmounts(
            Money.FromDecimal(175_000, "USD"),
            Money.FromDecimal(50_000, "USD"),
            null);
        loss1.UpdateStatus(LossStatus.Open);

        var loss2 = submission.AddLoss(DateTime.UtcNow.AddMonths(-18), "Fire in processing area");
        loss2.UpdateClaimInfo("CLM-2023-0445", CoverageType.PropertyDamage, "Industrial Insurance Co");
        loss2.UpdateAmounts(
            Money.FromDecimal(850_000, "USD"),
            Money.FromDecimal(0, "USD"),
            null);
        loss2.UpdateStatus(LossStatus.ClosedWithPayment);

        var loss3 = submission.AddLoss(DateTime.UtcNow.AddMonths(-12), "Environmental contamination incident");
        loss3.UpdateClaimInfo("CLM-2023-0667", CoverageType.GeneralLiability, "Industrial Insurance Co");
        loss3.UpdateAmounts(
            Money.FromDecimal(250_000, "USD"),
            Money.FromDecimal(500_000, "USD"),
            null);
        loss3.UpdateStatus(LossStatus.Open);

        var loss4 = submission.AddLoss(DateTime.UtcNow.AddMonths(-6), "Vehicle accident - serious injury");
        loss4.UpdateClaimInfo("CLM-2024-0123", CoverageType.Auto, "Industrial Insurance Co");
        loss4.UpdateAmounts(
            Money.FromDecimal(100_000, "USD"),
            Money.FromDecimal(400_000, "USD"),
            null);
        loss4.UpdateStatus(LossStatus.Open);
    }

    #endregion
}

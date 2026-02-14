using Vector.Domain.Routing.Aggregates;
using Vector.Domain.Routing.Entities;
using Vector.Domain.Routing.Enums;
using Vector.Domain.UnderwritingGuidelines.Aggregates;
using Vector.Domain.UnderwritingGuidelines.Enums;
using Vector.Domain.UnderwritingGuidelines.ValueObjects;
using Vector.EndToEnd.Tests.TestData;
using Vector.Infrastructure.Persistence;

namespace Vector.EndToEnd.Tests.Fixtures;

/// <summary>
/// Seeds comprehensive, production-like test data for end-to-end testing.
/// </summary>
public class ComprehensiveTestDataSeeder
{
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static readonly Dictionary<string, Guid> UnderwriterIds = new()
    {
        ["UW001"] = Guid.Parse("11111111-1111-1111-1111-111111111001"),
        ["UW002"] = Guid.Parse("11111111-1111-1111-1111-111111111002"),
        ["UW003"] = Guid.Parse("11111111-1111-1111-1111-111111111003"),
        ["UW004"] = Guid.Parse("11111111-1111-1111-1111-111111111004"),
        ["UW005"] = Guid.Parse("11111111-1111-1111-1111-111111111005"),
        ["UW006"] = Guid.Parse("11111111-1111-1111-1111-111111111006")
    };

    private static readonly Dictionary<string, Guid> ProducerIds = new()
    {
        ["MARSH001"] = Guid.Parse("22222222-2222-2222-2222-222222222001"),
        ["AON001"] = Guid.Parse("22222222-2222-2222-2222-222222222002"),
        ["WILLIS001"] = Guid.Parse("22222222-2222-2222-2222-222222222003"),
        ["HUB001"] = Guid.Parse("22222222-2222-2222-2222-222222222004"),
        ["AMWINS001"] = Guid.Parse("22222222-2222-2222-2222-222222222005")
    };

    public static Guid GetUnderwriterId(string code) => UnderwriterIds.GetValueOrDefault(code, UnderwriterIds["UW001"]);
    public static Guid GetProducerId(string code) => ProducerIds.GetValueOrDefault(code, ProducerIds["MARSH001"]);

    public async Task SeedAsync(VectorDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedUnderwritingGuidelinesAsync(context, cancellationToken);
        await SeedRoutingRulesAsync(context, cancellationToken);
        await SeedProducerUnderwriterPairingsAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUnderwritingGuidelinesAsync(VectorDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Set<UnderwritingGuideline>().AnyAsync(cancellationToken))
            return;

        var guidelines = CreateUnderwritingGuidelines();
        context.Set<UnderwritingGuideline>().AddRange(guidelines);
    }

    private List<UnderwritingGuideline> CreateUnderwritingGuidelines()
    {
        var guidelines = new List<UnderwritingGuideline>();

        // Property - Standard Commercial
        var propStandard = UnderwritingGuideline.Create(DefaultTenantId, "Property - Standard Commercial");
        propStandard.UpdateDetails("Property - Standard Commercial", "Standard property coverage for commercial buildings.");
        propStandard.SetApplicability("PropertyDamage", "US", "44,45,52,53,72");
        propStandard.SetEffectiveDates(DateTime.Today.AddYears(-1), DateTime.Today.AddYears(2));

        var propRule1 = propStandard.AddRule("TIV Maximum", RuleType.Eligibility, RuleAction.Decline, 100);
        propRule1.SetMessage("Total Insured Value exceeds maximum appetite");
        propRule1.AddCondition(RuleCondition.GreaterThan(RuleField.TotalInsuredValue, "100000000"));

        var propRule2 = propStandard.AddRule("Building Age Check", RuleType.Eligibility, RuleAction.Refer, 90);
        propRule2.SetMessage("Buildings over 50 years old require senior underwriter review");
        propRule2.AddCondition(RuleCondition.GreaterThan(RuleField.BuildingAge, "50"));

        propStandard.Activate();
        guidelines.Add(propStandard);

        // General Liability - Standard
        var glStandard = UnderwritingGuideline.Create(DefaultTenantId, "General Liability - Standard");
        glStandard.UpdateDetails("General Liability - Standard", "Standard GL coverage for commercial operations.");
        glStandard.SetApplicability("GeneralLiability", "US", null);
        glStandard.SetEffectiveDates(DateTime.Today.AddYears(-1), DateTime.Today.AddYears(2));

        var glRule1 = glStandard.AddRule("Revenue Maximum", RuleType.Eligibility, RuleAction.Decline, 100);
        glRule1.SetMessage("Revenue exceeds maximum");
        glRule1.AddCondition(RuleCondition.GreaterThan(RuleField.AnnualRevenue, "500000000"));

        glStandard.Activate();
        guidelines.Add(glStandard);

        // Professional Liability - Technology
        var plTech = UnderwritingGuideline.Create(DefaultTenantId, "Professional Liability - Technology");
        plTech.UpdateDetails("Professional Liability - Technology", "E&O coverage for technology companies.");
        plTech.SetApplicability("ProfessionalLiability", "US", "51,54");
        plTech.SetEffectiveDates(DateTime.Today.AddYears(-1), DateTime.Today.AddYears(2));

        var techRule1 = plTech.AddRule("Revenue Minimum", RuleType.Eligibility, RuleAction.Decline, 100);
        techRule1.SetMessage("Minimum revenue required");
        techRule1.AddCondition(RuleCondition.LessThan(RuleField.AnnualRevenue, "500000"));

        plTech.Activate();
        guidelines.Add(plTech);

        // Cyber Liability
        var cyber = UnderwritingGuideline.Create(DefaultTenantId, "Cyber Liability - Standard");
        cyber.UpdateDetails("Cyber Liability - Standard", "Cyber coverage for data breach and ransomware.");
        cyber.SetApplicability("Cyber", "US", null);
        cyber.SetEffectiveDates(DateTime.Today.AddYears(-1), DateTime.Today.AddYears(2));

        var cyberRule1 = cyber.AddRule("Revenue Maximum", RuleType.Eligibility, RuleAction.Decline, 100);
        cyberRule1.SetMessage("Revenue exceeds maximum for cyber coverage");
        cyberRule1.AddCondition(RuleCondition.GreaterThan(RuleField.AnnualRevenue, "1000000000"));

        cyber.Activate();
        guidelines.Add(cyber);

        return guidelines;
    }

    private async Task SeedRoutingRulesAsync(VectorDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Set<RoutingRule>().AnyAsync(cancellationToken))
            return;

        var rules = CreateRoutingRules();
        context.Set<RoutingRule>().AddRange(rules);
    }

    private List<RoutingRule> CreateRoutingRules()
    {
        var rules = new List<RoutingRule>();

        // High Value Manufacturing - Senior Property UW
        var rule1 = RoutingRule.Create("High Value Manufacturing", "Route high-value manufacturing to senior property underwriter", RoutingStrategy.Direct).Value;
        rule1.SetPriority(100);
        rule1.SetTargetUnderwriter(UnderwriterIds["UW001"], "Victoria Adams");
        rule1.AddCondition(Domain.Routing.ValueObjects.RoutingCondition.In(RuleField.NAICSCode, "31", "32", "33"));
        rule1.AddCondition(Domain.Routing.ValueObjects.RoutingCondition.GreaterThan(RuleField.TotalInsuredValue, "25000000"));
        rule1.Activate();
        rules.Add(rule1);

        // Professional Services - Professional Lines UW
        var rule2 = RoutingRule.Create("Professional Services", "Route professional liability to specialty underwriter", RoutingStrategy.Direct).Value;
        rule2.SetPriority(90);
        rule2.SetTargetUnderwriter(UnderwriterIds["UW003"], "Catherine Nguyen");
        rule2.AddCondition(Domain.Routing.ValueObjects.RoutingCondition.In(RuleField.NAICSCode, "54", "51"));
        rule2.Activate();
        rules.Add(rule2);

        // Construction - Casualty UW
        var rule3 = RoutingRule.Create("Construction Contractors", "Route construction risks to casualty underwriter", RoutingStrategy.Direct).Value;
        rule3.SetPriority(85);
        rule3.SetTargetUnderwriter(UnderwriterIds["UW002"], "Benjamin Hayes");
        rule3.AddCondition(Domain.Routing.ValueObjects.RoutingCondition.In(RuleField.NAICSCode, "236", "237", "238"));
        rule3.Activate();
        rules.Add(rule3);

        // Standard Property
        var rule4 = RoutingRule.Create("Standard Property", "Route standard property to property underwriter", RoutingStrategy.Direct).Value;
        rule4.SetPriority(40);
        rule4.SetTargetUnderwriter(UnderwriterIds["UW006"], "Eric Foster");
        rule4.AddCondition(Domain.Routing.ValueObjects.RoutingCondition.Equals(RuleField.CoverageType, "PropertyDamage"));
        rule4.Activate();
        rules.Add(rule4);

        // Catch-all - Manual Queue
        var rule5 = RoutingRule.Create("Unmatched Submissions", "Route unmatched submissions to manual queue", RoutingStrategy.ManualQueue).Value;
        rule5.SetPriority(1);
        rule5.Activate();
        rules.Add(rule5);

        return rules;
    }

    private async Task SeedProducerUnderwriterPairingsAsync(VectorDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Set<ProducerUnderwriterPairing>().AnyAsync(cancellationToken))
            return;

        var pairings = CreateProducerUnderwriterPairings();
        context.Set<ProducerUnderwriterPairing>().AddRange(pairings);
    }

    private List<ProducerUnderwriterPairing> CreateProducerUnderwriterPairings()
    {
        var pairings = new List<ProducerUnderwriterPairing>
        {
            CreatePairing("MARSH001", "Marsh McLennan Agency", "UW001", "Victoria Adams", 100),
            CreatePairing("AON001", "Aon Risk Solutions", "UW001", "Victoria Adams", 95),
            CreatePairing("WILLIS001", "Willis Towers Watson", "UW002", "Benjamin Hayes", 90),
            CreatePairing("HUB001", "Hub International Northeast", "UW006", "Eric Foster", 70),
            CreatePairing("AMWINS001", "AmWINS Group", "UW003", "Catherine Nguyen", 75)
        };

        return pairings;
    }

    private ProducerUnderwriterPairing CreatePairing(string producerCode, string producerName, string uwCode, string uwName, int priority)
    {
        var pairing = ProducerUnderwriterPairing.Create(
            ProducerIds[producerCode],
            producerName,
            UnderwriterIds[uwCode],
            uwName);

        pairing.SetPriority(priority);
        pairing.SetEffectivePeriod(DateTime.Today.AddYears(-1), DateTime.Today.AddYears(2));
        pairing.Activate();

        return pairing;
    }
}

namespace Vector.EndToEnd.Tests.TestData;

/// <summary>
/// Contains realistic test data for insurance submission testing.
/// Data patterns based on real-world P&C insurance industry conventions.
/// </summary>
public static class RealisticTestData
{
    #region MGAs / Carriers

    public static readonly MgaProfile[] Mgas =
    [
        new("AmRisc", "AmRisc Group", "Property & Casualty", "FL,TX,LA,SC,NC,GA,AL,MS", "Wind,Property,Inland Marine"),
        new("Burns & Wilcox", "Burns & Wilcox", "Wholesale Specialty", "US", "Property,GL,Professional,Excess"),
        new("CRC Insurance", "CRC Group", "Wholesale Brokerage", "US", "Property,Casualty,Professional,Specialty"),
        new("RSG Underwriting", "Ryan Specialty Group", "Specialty MGA", "US", "Property,GL,Professional,Environmental"),
        new("Markel Specialty", "Markel Corporation", "Specialty Insurance", "US", "GL,Professional,Workers Comp"),
        new("Monarch E&S", "Monarch E&S Insurance", "E&S Markets", "US", "Hard-to-place,Excess,Surplus"),
        new("Worldwide Facilities", "Worldwide Facilities LLC", "Wholesale Broker", "US,CA", "Property,Casualty,Professional"),
        new("All Risks Ltd", "All Risks Ltd", "Wholesale Specialty", "US", "Property,Inland Marine,GL")
    ];

    public record MgaProfile(string Code, string Name, string Specialty, string States, string CoverageTypes);

    #endregion

    #region Producers / Brokers

    public static readonly ProducerProfile[] Producers =
    [
        // Large National Brokers
        new("MARSH001", "Marsh McLennan Agency", "John Richardson", "john.richardson@marsh.com", "Large National", "NY"),
        new("AON001", "Aon Risk Solutions", "Sarah Chen", "sarah.chen@aon.com", "Large National", "IL"),
        new("WILLIS001", "Willis Towers Watson", "Michael Torres", "michael.torres@wtwco.com", "Large National", "TX"),
        new("GALLAG001", "Gallagher Insurance", "Jennifer Lopez", "jlopez@ajg.com", "Large National", "CA"),
        new("LOCKTON001", "Lockton Companies", "David Kim", "dkim@lockton.com", "Large National", "MO"),

        // Regional Brokers
        new("HUB001", "Hub International Northeast", "Robert Williams", "rwilliams@hubinternational.com", "Regional", "NY"),
        new("ACRISURE001", "Acrisure LLC", "Emily Johnson", "ejohnson@acrisure.com", "Regional", "MI"),
        new("ALLIANT001", "Alliant Insurance Services", "Christopher Brown", "cbrown@alliant.com", "Regional", "CA"),
        new("RISK001", "Risk Strategies Company", "Amanda Davis", "adavis@risk-strategies.com", "Regional", "MA"),
        new("ASSURE001", "AssuredPartners", "James Wilson", "jwilson@assuredpartners.com", "Regional", "FL"),

        // Specialty/Niche Brokers
        new("AMWINS001", "AmWINS Group", "Patricia Martinez", "pmartinez@amwins.com", "Specialty", "NC"),
        new("RT001", "RT Specialty", "Thomas Anderson", "tanderson@rtspecialty.com", "Specialty", "IL"),
        new("BEECHER001", "Beecher Carlson", "Nancy Taylor", "ntaylor@beechercarlson.com", "Specialty", "GA"),
        new("USIB001", "USI Insurance Services", "Daniel Jackson", "djackson@usi.com", "Specialty", "NY"),
        new("BROWN001", "Brown & Brown", "Michelle White", "mwhite@bbrown.com", "Specialty", "FL"),

        // Local/Boutique Agencies
        new("METRO001", "Metro Insurance Agency", "Kevin Harris", "kharris@metroins.com", "Local", "TX"),
        new("COASTAL001", "Coastal Risk Advisors", "Lisa Thompson", "lthompson@coastalrisk.com", "Local", "FL"),
        new("PACIFIC001", "Pacific Insurance Partners", "Ryan Clark", "rclark@pacificins.com", "Local", "CA"),
        new("MIDWEST001", "Midwest Commercial Insurance", "Stephanie Lee", "slee@midwestcommercial.com", "Local", "OH"),
        new("EMPIRE001", "Empire State Insurance Group", "Andrew Robinson", "arobinson@empireins.com", "Local", "NY")
    ];

    public record ProducerProfile(string Code, string Name, string ContactName, string Email, string Type, string State);

    #endregion

    #region Underwriters

    public static readonly UnderwriterProfile[] Underwriters =
    [
        // Senior Underwriters
        new("UW001", "Victoria Adams", "Senior Underwriter", "Property", 15, ["Property", "Inland Marine"]),
        new("UW002", "Benjamin Hayes", "Senior Underwriter", "Casualty", 12, ["GL", "Excess", "Umbrella"]),
        new("UW003", "Catherine Nguyen", "Senior Underwriter", "Professional Lines", 10, ["D&O", "E&O", "Cyber"]),
        new("UW004", "Marcus Johnson", "Senior Underwriter", "Workers Comp", 14, ["Workers Comp"]),
        new("UW005", "Diana Rodriguez", "Senior Underwriter", "Environmental", 8, ["Environmental", "Pollution"]),

        // Underwriters
        new("UW006", "Eric Foster", "Underwriter", "Property", 6, ["Property", "BOP"]),
        new("UW007", "Samantha Wright", "Underwriter", "Casualty", 5, ["GL", "Products"]),
        new("UW008", "Jonathan Park", "Underwriter", "Professional Lines", 4, ["E&O", "Cyber"]),
        new("UW009", "Rachel Green", "Underwriter", "Multi-Line", 7, ["Property", "GL", "Auto"]),
        new("UW010", "Christopher Lee", "Underwriter", "Specialty", 5, ["Inland Marine", "Builders Risk"]),

        // Junior Underwriters
        new("UW011", "Ashley Turner", "Junior Underwriter", "Property", 2, ["Property"]),
        new("UW012", "Brandon Cooper", "Junior Underwriter", "Casualty", 1, ["GL"]),
        new("UW013", "Megan Phillips", "Junior Underwriter", "Professional Lines", 2, ["Cyber"]),
        new("UW014", "Tyler Morgan", "Junior Underwriter", "Multi-Line", 1, ["Property", "GL"]),
        new("UW015", "Olivia Baker", "Junior Underwriter", "Workers Comp", 2, ["Workers Comp"])
    ];

    public record UnderwriterProfile(string Code, string Name, string Title, string Specialty, int YearsExperience, string[] CoverageTypes);

    #endregion

    #region NAICS Industry Codes

    public static readonly IndustryProfile[] Industries =
    [
        // Manufacturing
        new("311", "Food Manufacturing", "Manufacturing"),
        new("312", "Beverage and Tobacco Product Manufacturing", "Manufacturing"),
        new("321", "Wood Product Manufacturing", "Manufacturing"),
        new("322", "Paper Manufacturing", "Manufacturing"),
        new("323", "Printing and Related Support Activities", "Manufacturing"),
        new("324", "Petroleum and Coal Products Manufacturing", "Manufacturing"),
        new("325", "Chemical Manufacturing", "Manufacturing"),
        new("326", "Plastics and Rubber Products Manufacturing", "Manufacturing"),
        new("331", "Primary Metal Manufacturing", "Manufacturing"),
        new("332", "Fabricated Metal Product Manufacturing", "Manufacturing"),
        new("333", "Machinery Manufacturing", "Manufacturing"),
        new("334", "Computer and Electronic Product Manufacturing", "Manufacturing"),
        new("335", "Electrical Equipment Manufacturing", "Manufacturing"),
        new("336", "Transportation Equipment Manufacturing", "Manufacturing"),
        new("337", "Furniture and Related Product Manufacturing", "Manufacturing"),
        new("339", "Miscellaneous Manufacturing", "Manufacturing"),

        // Retail Trade
        new("441", "Motor Vehicle and Parts Dealers", "Retail"),
        new("442", "Furniture and Home Furnishings Stores", "Retail"),
        new("443", "Electronics and Appliance Stores", "Retail"),
        new("444", "Building Material and Garden Equipment Dealers", "Retail"),
        new("445", "Food and Beverage Stores", "Retail"),
        new("446", "Health and Personal Care Stores", "Retail"),
        new("447", "Gasoline Stations", "Retail"),
        new("448", "Clothing and Clothing Accessories Stores", "Retail"),
        new("451", "Sporting Goods and Musical Instrument Stores", "Retail"),
        new("452", "General Merchandise Stores", "Retail"),
        new("453", "Miscellaneous Store Retailers", "Retail"),
        new("454", "Nonstore Retailers", "Retail"),

        // Services
        new("511", "Publishing Industries", "Services"),
        new("512", "Motion Picture and Sound Recording", "Services"),
        new("517", "Telecommunications", "Services"),
        new("518", "Data Processing and Hosting Services", "Services"),
        new("519", "Other Information Services", "Services"),
        new("521", "Monetary Authorities", "Finance"),
        new("522", "Credit Intermediation", "Finance"),
        new("523", "Securities and Investments", "Finance"),
        new("524", "Insurance Carriers and Related Activities", "Finance"),
        new("531", "Real Estate", "Real Estate"),
        new("532", "Rental and Leasing Services", "Services"),
        new("541", "Professional, Scientific, and Technical Services", "Professional"),
        new("551", "Management of Companies", "Professional"),
        new("561", "Administrative and Support Services", "Services"),
        new("562", "Waste Management and Remediation Services", "Services"),

        // Construction
        new("236", "Construction of Buildings", "Construction"),
        new("237", "Heavy and Civil Engineering Construction", "Construction"),
        new("238", "Specialty Trade Contractors", "Construction"),

        // Healthcare
        new("621", "Ambulatory Health Care Services", "Healthcare"),
        new("622", "Hospitals", "Healthcare"),
        new("623", "Nursing and Residential Care Facilities", "Healthcare"),
        new("624", "Social Assistance", "Healthcare"),

        // Hospitality
        new("721", "Accommodation", "Hospitality"),
        new("722", "Food Services and Drinking Places", "Hospitality"),

        // Transportation
        new("481", "Air Transportation", "Transportation"),
        new("482", "Rail Transportation", "Transportation"),
        new("483", "Water Transportation", "Transportation"),
        new("484", "Truck Transportation", "Transportation"),
        new("485", "Transit and Ground Passenger Transportation", "Transportation"),
        new("488", "Support Activities for Transportation", "Transportation"),
        new("492", "Couriers and Messengers", "Transportation"),
        new("493", "Warehousing and Storage", "Transportation")
    ];

    public record IndustryProfile(string NaicsCode, string Description, string Sector);

    #endregion

    #region Construction Types

    public static readonly string[] ConstructionTypes =
    [
        "Frame",
        "Joisted Masonry",
        "Non-Combustible",
        "Masonry Non-Combustible",
        "Modified Fire Resistive",
        "Fire Resistive",
        "Mixed",
        "Superior Construction"
    ];

    #endregion

    #region Occupancy Types

    public static readonly string[] OccupancyTypes =
    [
        "Office",
        "Retail Store",
        "Restaurant",
        "Warehouse",
        "Manufacturing Plant",
        "Distribution Center",
        "Medical Office",
        "Dental Office",
        "Bank Branch",
        "Auto Dealership",
        "Auto Repair Shop",
        "Hotel/Motel",
        "Apartment Building",
        "Shopping Center",
        "Strip Mall",
        "Industrial Building",
        "Cold Storage",
        "Data Center",
        "Call Center",
        "Research Laboratory",
        "Fitness Center",
        "Daycare Center",
        "Church/Religious",
        "School/Educational",
        "Parking Garage",
        "Mixed Use"
    ];

    #endregion

    #region US States

    public static readonly StateInfo[] States =
    [
        new("AL", "Alabama", "Central"),
        new("AK", "Alaska", "Pacific"),
        new("AZ", "Arizona", "Mountain"),
        new("AR", "Arkansas", "Central"),
        new("CA", "California", "Pacific"),
        new("CO", "Colorado", "Mountain"),
        new("CT", "Connecticut", "Northeast"),
        new("DE", "Delaware", "Northeast"),
        new("FL", "Florida", "Southeast"),
        new("GA", "Georgia", "Southeast"),
        new("HI", "Hawaii", "Pacific"),
        new("ID", "Idaho", "Mountain"),
        new("IL", "Illinois", "Midwest"),
        new("IN", "Indiana", "Midwest"),
        new("IA", "Iowa", "Midwest"),
        new("KS", "Kansas", "Central"),
        new("KY", "Kentucky", "Central"),
        new("LA", "Louisiana", "Gulf"),
        new("ME", "Maine", "Northeast"),
        new("MD", "Maryland", "Northeast"),
        new("MA", "Massachusetts", "Northeast"),
        new("MI", "Michigan", "Midwest"),
        new("MN", "Minnesota", "Midwest"),
        new("MS", "Mississippi", "Gulf"),
        new("MO", "Missouri", "Central"),
        new("MT", "Montana", "Mountain"),
        new("NE", "Nebraska", "Central"),
        new("NV", "Nevada", "Mountain"),
        new("NH", "New Hampshire", "Northeast"),
        new("NJ", "New Jersey", "Northeast"),
        new("NM", "New Mexico", "Mountain"),
        new("NY", "New York", "Northeast"),
        new("NC", "North Carolina", "Southeast"),
        new("ND", "North Dakota", "Central"),
        new("OH", "Ohio", "Midwest"),
        new("OK", "Oklahoma", "Central"),
        new("OR", "Oregon", "Pacific"),
        new("PA", "Pennsylvania", "Northeast"),
        new("RI", "Rhode Island", "Northeast"),
        new("SC", "South Carolina", "Southeast"),
        new("SD", "South Dakota", "Central"),
        new("TN", "Tennessee", "Central"),
        new("TX", "Texas", "Gulf"),
        new("UT", "Utah", "Mountain"),
        new("VT", "Vermont", "Northeast"),
        new("VA", "Virginia", "Southeast"),
        new("WA", "Washington", "Pacific"),
        new("WV", "West Virginia", "Central"),
        new("WI", "Wisconsin", "Midwest"),
        new("WY", "Wyoming", "Mountain"),
        new("DC", "District of Columbia", "Northeast")
    ];

    public record StateInfo(string Code, string Name, string Region);

    #endregion

    #region Insurance Carriers (Prior Carriers)

    public static readonly string[] InsuranceCarriers =
    [
        "Travelers",
        "Hartford",
        "Liberty Mutual",
        "CNA",
        "Chubb",
        "AIG",
        "Zurich",
        "Nationwide",
        "State Farm",
        "Progressive",
        "Berkshire Hathaway",
        "USAA",
        "Farmers",
        "American Family",
        "Auto-Owners",
        "Erie Insurance",
        "Hanover Insurance",
        "Cincinnati Insurance",
        "Westfield Insurance",
        "Great American",
        "Markel",
        "Tokio Marine",
        "Arch Insurance",
        "RLI Insurance",
        "Philadelphia Insurance",
        "Selective Insurance",
        "EMC Insurance",
        "West Bend Mutual",
        "Frankenmuth Insurance",
        "USLI",
        "Axis Capital",
        "Argo Group",
        "Kinsale Insurance",
        "James River Insurance"
    ];

    #endregion

    #region Company Name Patterns

    public static readonly string[] CompanyPrefixes =
    [
        "Advanced", "Allied", "American", "Atlantic", "Blue Ridge", "Capital", "Central",
        "Coastal", "Continental", "Delta", "Eagle", "Eastern", "Elite", "Empire",
        "First", "Global", "Golden", "Great", "Guardian", "Heritage", "Highland",
        "Imperial", "Interstate", "Liberty", "Lone Star", "Magnolia", "Metro",
        "Midwest", "Mountain", "National", "Northern", "Northwest", "Ocean",
        "Pacific", "Patriot", "Peak", "Pinnacle", "Pioneer", "Premier", "Prime",
        "Quality", "Regional", "Rocky Mountain", "Royal", "Sierra", "Silver",
        "Southern", "Southwest", "Summit", "Sun", "Superior", "United", "Valley",
        "Western", "Worldwide"
    ];

    public static readonly string[] CompanySuffixes =
    [
        "Industries", "Manufacturing", "Corp", "Corporation", "Inc", "LLC", "LP",
        "Enterprises", "Group", "Holdings", "Partners", "Associates", "Solutions",
        "Services", "Systems", "Technologies", "Products", "Company", "Co",
        "International", "America", "USA", "North America", "Global"
    ];

    public static readonly string[] IndustryTerms =
    [
        "Aerospace", "Automotive", "Chemical", "Construction", "Distribution",
        "Electronics", "Energy", "Engineering", "Equipment", "Fabrication",
        "Food", "Freight", "Healthcare", "Hospitality", "Industrial",
        "Logistics", "Marine", "Materials", "Medical", "Metal", "Packaging",
        "Pharmaceutical", "Plastics", "Precision", "Processing", "Retail",
        "Steel", "Supply", "Technology", "Textile", "Transport", "Warehouse"
    ];

    #endregion

    #region Street Names

    public static readonly string[] StreetNames =
    [
        "Main", "Oak", "Maple", "Cedar", "Pine", "Elm", "Washington", "Lincoln",
        "Jefferson", "Franklin", "Madison", "Jackson", "Industrial", "Commerce",
        "Enterprise", "Corporate", "Business", "Technology", "Innovation", "Research",
        "Park", "Center", "Gateway", "Lakeside", "Riverside", "Hillside", "Valley",
        "Sunrise", "Sunset", "Meadow", "Forest", "Spring", "Autumn", "Winter"
    ];

    public static readonly string[] StreetTypes =
    [
        "Street", "Avenue", "Boulevard", "Drive", "Road", "Lane", "Way", "Parkway",
        "Circle", "Court", "Place", "Trail", "Highway", "Pike"
    ];

    #endregion

    #region Email Templates

    public static readonly string[] EmailSubjectTemplates =
    [
        "New Submission - {InsuredName}",
        "Submission: {InsuredName} - {CoverageTypes}",
        "Quote Request - {InsuredName}",
        "{InsuredName} - New Business Submission",
        "FW: {InsuredName} Renewal Submission",
        "RE: {InsuredName} - Updated Submission",
        "{CoverageTypes} Submission - {InsuredName}",
        "New Account - {InsuredName} ({State})",
        "Submission Package - {InsuredName}",
        "{ProducerName} - New Submission for {InsuredName}"
    ];

    public static readonly string[] EmailBodyTemplates =
    [
        @"Dear Underwriting Team,

Please find attached the submission documents for our client:

Insured: {InsuredName}
Coverage Requested: {CoverageTypes}
Effective Date: {EffectiveDate}
Expiration Date: {ExpirationDate}

Attached Documents:
{AttachmentList}

Please let me know if you need any additional information.

Best regards,
{ProducerContact}
{ProducerName}
{ProducerEmail}",

        @"Good morning,

I am pleased to submit the following new business opportunity:

Named Insured: {InsuredName}
Industry: {Industry}
Location(s): {LocationCount} location(s) in {States}
Lines of Coverage: {CoverageTypes}
Requested Effective: {EffectiveDate}

The following documents are attached:
{AttachmentList}

Target premium based on expiring: {TargetPremium}

Thank you for your consideration. I look forward to your response.

{ProducerContact}
{ProducerName}
Phone: {ProducerPhone}",

        @"Underwriting,

Attached please find a new submission for your review.

Account Details:
- Insured Name: {InsuredName}
- DBA: {DbaName}
- FEIN: {Fein}
- Years in Business: {YearsInBusiness}
- Annual Revenue: {AnnualRevenue}

Coverage Request:
{CoverageDetails}

Loss History Summary:
- Total Claims (5 years): {ClaimCount}
- Total Incurred: {TotalIncurred}

Documents Enclosed:
{AttachmentList}

Please advise on appetite and any additional information needed.

Thanks,
{ProducerContact}",

        @"Hi Team,

New submission attached for {InsuredName}.

Quick summary:
• {LocationCount} locations across {States}
• TIV: {TotalTIV}
• Coverage needed: {CoverageTypes}
• Eff: {EffectiveDate}

Full submission package attached. Let me know if you need anything else.

{ProducerContact}
{ProducerName}",

        @"Please see the attached submission materials for {InsuredName}.

This is a {NewOrRenewal} account currently with {PriorCarrier}.

Key metrics:
- Total Insured Value: {TotalTIV}
- Locations: {LocationCount}
- Loss Ratio (5yr): {LossRatio}%

ACORD applications and supporting documents attached.

{ProducerContact}
{ProducerEmail}"
    ];

    #endregion
}

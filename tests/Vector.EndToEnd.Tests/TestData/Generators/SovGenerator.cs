using ClosedXML.Excel;

namespace Vector.EndToEnd.Tests.TestData.Generators;

/// <summary>
/// Generates realistic Statement of Values (SOV) Excel files based on AmRisc format.
/// </summary>
public class SovGenerator
{
    /// <summary>
    /// SOV column definitions based on AmRisc template format.
    /// </summary>
    private static readonly SovColumn[] Columns =
    [
        new("Loc #", "A", typeof(int)),
        new("Bldg #", "B", typeof(int)),
        new("Location Name", "C", typeof(string)),
        new("Street Address", "D", typeof(string)),
        new("City", "E", typeof(string)),
        new("State", "F", typeof(string)),
        new("Zip Code", "G", typeof(string)),
        new("County", "H", typeof(string)),
        new("Country", "I", typeof(string)),
        new("Occupancy Description", "J", typeof(string)),
        new("Construction Type", "K", typeof(string)),
        new("Year Built", "L", typeof(int)),
        new("Stories", "M", typeof(int)),
        new("Square Footage", "N", typeof(int)),
        new("Protection Class", "O", typeof(string)),
        new("Sprinklered", "P", typeof(string)),
        new("Fire Alarm", "Q", typeof(string)),
        new("Security System", "R", typeof(string)),
        new("Roof Type", "S", typeof(string)),
        new("Roof Age", "T", typeof(int)),
        new("Distance to Coast (miles)", "U", typeof(decimal)),
        new("Distance to Fire Hydrant (ft)", "V", typeof(int)),
        new("Distance to Fire Station (miles)", "W", typeof(decimal)),
        new("Flood Zone", "X", typeof(string)),
        new("Building Value", "Y", typeof(decimal)),
        new("Contents Value", "Z", typeof(decimal)),
        new("BI/EE Value", "AA", typeof(decimal)),
        new("Total Insured Value", "AB", typeof(decimal)),
        new("Building Limit", "AC", typeof(decimal)),
        new("Contents Limit", "AD", typeof(decimal)),
        new("BI/EE Limit", "AE", typeof(decimal)),
        new("Total Limit", "AF", typeof(decimal)),
        new("Deductible", "AG", typeof(decimal)),
        new("Wind/Hail Deductible %", "AH", typeof(decimal)),
        new("Named Storm Deductible %", "AI", typeof(decimal)),
        new("Earthquake Deductible %", "AJ", typeof(decimal)),
        new("Flood Deductible", "AK", typeof(decimal)),
        new("Valuation Type", "AL", typeof(string)),
        new("Coinsurance %", "AM", typeof(int)),
        new("Agreed Value", "AN", typeof(string)),
        new("Blanket #", "AO", typeof(string)),
        new("Notes", "AP", typeof(string))
    ];

    private readonly Random _random;

    public SovGenerator(int seed = 12345)
    {
        _random = new Random(seed);
    }

    /// <summary>
    /// Generates an SOV Excel workbook for the given locations.
    /// </summary>
    public byte[] GenerateSovWorkbook(List<SubmissionLocation> locations, SubmissionInsured insured)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("SOV-APP");

        // Add header row
        AddHeaders(worksheet);

        // Add data rows
        int row = 2;
        foreach (var location in locations)
        {
            AddLocationRow(worksheet, row, location, insured);
            row++;
        }

        // Add summary section
        AddSummary(worksheet, row + 1, locations);

        // Format worksheet
        FormatWorksheet(worksheet, row - 1);

        // Save to memory stream
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Generates a large SOV for stress testing.
    /// </summary>
    public byte[] GenerateLargeSov(int locationCount, SubmissionInsured insured)
    {
        var generator = new SubmissionGenerator(_random.Next());
        var locations = new List<SubmissionLocation>();

        // Generate many locations
        var states = RealisticTestData.States.OrderBy(_ => _random.Next()).Take(Math.Min(locationCount / 5, 20)).ToArray();

        for (int i = 0; i < locationCount; i++)
        {
            var state = states[i % states.Length];
            locations.Add(new SubmissionLocation
            {
                LocationNumber = i + 1,
                Street = $"{_random.Next(100, 9999)} {RealisticTestData.StreetNames[_random.Next(RealisticTestData.StreetNames.Length)]} {RealisticTestData.StreetTypes[_random.Next(RealisticTestData.StreetTypes.Length)]}",
                City = $"City{i % 100}",
                State = state.Code,
                ZipCode = $"{_random.Next(10000, 99999)}",
                OccupancyType = RealisticTestData.OccupancyTypes[_random.Next(RealisticTestData.OccupancyTypes.Length)],
                ConstructionType = RealisticTestData.ConstructionTypes[_random.Next(RealisticTestData.ConstructionTypes.Length)],
                YearBuilt = _random.Next(1950, 2024),
                SquareFootage = _random.Next(5000, 500000),
                NumberOfStories = _random.Next(1, 20),
                BuildingValue = _random.Next(500000, 50000000),
                ContentsValue = _random.Next(100000, 10000000),
                BusinessIncomeValue = _random.Next(50000, 5000000),
                HasSprinklers = _random.NextDouble() > 0.3,
                HasFireAlarm = _random.NextDouble() > 0.2,
                HasSecuritySystem = _random.NextDouble() > 0.4,
                ProtectionClass = _random.Next(1, 10).ToString()
            });
        }

        return GenerateSovWorkbook(locations, insured);
    }

    private void AddHeaders(IXLWorksheet worksheet)
    {
        for (int i = 0; i < Columns.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = Columns[i].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }
    }

    private void AddLocationRow(IXLWorksheet worksheet, int row, SubmissionLocation location, SubmissionInsured insured)
    {
        var col = 1;

        // Location identifiers
        worksheet.Cell(row, col++).Value = location.LocationNumber;
        worksheet.Cell(row, col++).Value = 1; // Building #
        worksheet.Cell(row, col++).Value = $"{insured.Name} - Location {location.LocationNumber}";

        // Address
        worksheet.Cell(row, col++).Value = location.Street;
        worksheet.Cell(row, col++).Value = location.City;
        worksheet.Cell(row, col++).Value = location.State;
        worksheet.Cell(row, col++).Value = location.ZipCode;
        worksheet.Cell(row, col++).Value = GetCounty(location.State);
        worksheet.Cell(row, col++).Value = "USA";

        // Building characteristics
        worksheet.Cell(row, col++).Value = location.OccupancyType;
        worksheet.Cell(row, col++).Value = location.ConstructionType;
        worksheet.Cell(row, col++).Value = location.YearBuilt;
        worksheet.Cell(row, col++).Value = location.NumberOfStories;
        worksheet.Cell(row, col++).Value = location.SquareFootage;

        // Protection
        worksheet.Cell(row, col++).Value = location.ProtectionClass;
        worksheet.Cell(row, col++).Value = location.HasSprinklers ? "Yes" : "No";
        worksheet.Cell(row, col++).Value = location.HasFireAlarm ? "Yes" : "No";
        worksheet.Cell(row, col++).Value = location.HasSecuritySystem ? "Yes" : "No";

        // Roof
        var roofType = GetRoofType(location.ConstructionType);
        worksheet.Cell(row, col++).Value = roofType;
        worksheet.Cell(row, col++).Value = Math.Max(0, DateTime.Now.Year - location.YearBuilt - _random.Next(0, 20));

        // Distances
        worksheet.Cell(row, col++).Value = Math.Round(_random.NextDouble() * 50, 1); // Coast
        worksheet.Cell(row, col++).Value = _random.Next(100, 2000); // Hydrant
        worksheet.Cell(row, col++).Value = Math.Round(_random.NextDouble() * 10, 1); // Fire station

        // Flood zone
        worksheet.Cell(row, col++).Value = GetFloodZone();

        // Values
        worksheet.Cell(row, col++).Value = location.BuildingValue;
        worksheet.Cell(row, col++).Value = location.ContentsValue;
        worksheet.Cell(row, col++).Value = location.BusinessIncomeValue;
        var totalTiv = location.BuildingValue + location.ContentsValue + location.BusinessIncomeValue;
        worksheet.Cell(row, col++).Value = totalTiv;

        // Limits (often same as values)
        worksheet.Cell(row, col++).Value = location.BuildingValue;
        worksheet.Cell(row, col++).Value = location.ContentsValue;
        worksheet.Cell(row, col++).Value = location.BusinessIncomeValue;
        worksheet.Cell(row, col++).Value = totalTiv;

        // Deductibles
        worksheet.Cell(row, col++).Value = GetDeductible(totalTiv);
        worksheet.Cell(row, col++).Value = GetWindDeductiblePercent(location.State);
        worksheet.Cell(row, col++).Value = GetNamedStormDeductiblePercent(location.State);
        worksheet.Cell(row, col++).Value = GetEarthquakeDeductiblePercent(location.State);
        worksheet.Cell(row, col++).Value = GetFloodDeductible(totalTiv);

        // Valuation
        worksheet.Cell(row, col++).Value = "Replacement Cost";
        worksheet.Cell(row, col++).Value = 100;
        worksheet.Cell(row, col++).Value = "Yes";

        // Blanket
        worksheet.Cell(row, col++).Value = "BL-001";

        // Notes
        worksheet.Cell(row, col++).Value = "";

        // Format currency columns
        FormatCurrencyCell(worksheet.Cell(row, 25)); // Building Value
        FormatCurrencyCell(worksheet.Cell(row, 26)); // Contents Value
        FormatCurrencyCell(worksheet.Cell(row, 27)); // BI Value
        FormatCurrencyCell(worksheet.Cell(row, 28)); // Total TIV
        FormatCurrencyCell(worksheet.Cell(row, 29)); // Building Limit
        FormatCurrencyCell(worksheet.Cell(row, 30)); // Contents Limit
        FormatCurrencyCell(worksheet.Cell(row, 31)); // BI Limit
        FormatCurrencyCell(worksheet.Cell(row, 32)); // Total Limit
        FormatCurrencyCell(worksheet.Cell(row, 33)); // Deductible
        FormatCurrencyCell(worksheet.Cell(row, 37)); // Flood Deductible
    }

    private void AddSummary(IXLWorksheet worksheet, int startRow, List<SubmissionLocation> locations)
    {
        var summaryRow = startRow + 2;

        worksheet.Cell(summaryRow, 1).Value = "SUMMARY";
        worksheet.Cell(summaryRow, 1).Style.Font.Bold = true;

        summaryRow++;
        worksheet.Cell(summaryRow, 1).Value = "Total Locations:";
        worksheet.Cell(summaryRow, 2).Value = locations.Count;

        summaryRow++;
        worksheet.Cell(summaryRow, 1).Value = "Total Building Value:";
        worksheet.Cell(summaryRow, 2).Value = locations.Sum(l => l.BuildingValue);
        FormatCurrencyCell(worksheet.Cell(summaryRow, 2));

        summaryRow++;
        worksheet.Cell(summaryRow, 1).Value = "Total Contents Value:";
        worksheet.Cell(summaryRow, 2).Value = locations.Sum(l => l.ContentsValue);
        FormatCurrencyCell(worksheet.Cell(summaryRow, 2));

        summaryRow++;
        worksheet.Cell(summaryRow, 1).Value = "Total BI/EE Value:";
        worksheet.Cell(summaryRow, 2).Value = locations.Sum(l => l.BusinessIncomeValue);
        FormatCurrencyCell(worksheet.Cell(summaryRow, 2));

        summaryRow++;
        worksheet.Cell(summaryRow, 1).Value = "Total Insured Value:";
        worksheet.Cell(summaryRow, 2).Value = locations.Sum(l => l.BuildingValue + l.ContentsValue + l.BusinessIncomeValue);
        worksheet.Cell(summaryRow, 2).Style.Font.Bold = true;
        FormatCurrencyCell(worksheet.Cell(summaryRow, 2));

        summaryRow += 2;
        worksheet.Cell(summaryRow, 1).Value = "States:";
        worksheet.Cell(summaryRow, 2).Value = string.Join(", ", locations.Select(l => l.State).Distinct().OrderBy(s => s));
    }

    private void FormatWorksheet(IXLWorksheet worksheet, int lastDataRow)
    {
        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Freeze header row
        worksheet.SheetView.FreezeRows(1);

        // Add filters
        worksheet.RangeUsed()?.SetAutoFilter();

        // Set print area
        worksheet.PageSetup.PrintAreas.Add(1, 1, lastDataRow, Columns.Length);
        worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        worksheet.PageSetup.FitToPages(1, 0);
    }

    private static void FormatCurrencyCell(IXLCell cell)
    {
        cell.Style.NumberFormat.Format = "$#,##0";
    }

    private string GetCounty(string state)
    {
        // Return a plausible county name
        return state switch
        {
            "TX" => _random.Next(3) switch { 0 => "Harris", 1 => "Dallas", _ => "Bexar" },
            "CA" => _random.Next(3) switch { 0 => "Los Angeles", 1 => "San Diego", _ => "Orange" },
            "FL" => _random.Next(3) switch { 0 => "Miami-Dade", 1 => "Broward", _ => "Palm Beach" },
            "NY" => _random.Next(3) switch { 0 => "New York", 1 => "Kings", _ => "Queens" },
            _ => "County"
        };
    }

    private string GetRoofType(string constructionType)
    {
        return constructionType switch
        {
            "Fire Resistive" or "Modified Fire Resistive" => "Built-Up",
            "Non-Combustible" or "Masonry Non-Combustible" => "Metal",
            "Frame" => "Asphalt Shingle",
            _ => _random.Next(4) switch { 0 => "Built-Up", 1 => "Metal", 2 => "Single Ply", _ => "Asphalt Shingle" }
        };
    }

    private string GetFloodZone()
    {
        var roll = _random.NextDouble();
        return roll switch
        {
            < 0.6 => "X",
            < 0.75 => "X500",
            < 0.85 => "AE",
            < 0.92 => "A",
            < 0.97 => "VE",
            _ => "V"
        };
    }

    private decimal GetDeductible(decimal tiv)
    {
        if (tiv > 50_000_000) return 100_000;
        if (tiv > 10_000_000) return 50_000;
        if (tiv > 5_000_000) return 25_000;
        if (tiv > 1_000_000) return 10_000;
        return 5_000;
    }

    private decimal GetWindDeductiblePercent(string state)
    {
        // Higher wind deductibles in coastal states
        return state switch
        {
            "FL" or "LA" or "TX" or "SC" or "NC" or "GA" or "AL" or "MS" => _random.Next(2, 5),
            _ => 0
        };
    }

    private decimal GetNamedStormDeductiblePercent(string state)
    {
        return state switch
        {
            "FL" or "LA" or "TX" or "SC" or "NC" => _random.Next(2, 5),
            _ => 0
        };
    }

    private decimal GetEarthquakeDeductiblePercent(string state)
    {
        return state switch
        {
            "CA" => _random.Next(5, 15),
            "WA" or "OR" => _random.Next(2, 10),
            "MO" or "TN" or "AR" => _random.Next(2, 5), // New Madrid zone
            _ => 0
        };
    }

    private decimal GetFloodDeductible(decimal tiv)
    {
        return GetDeductible(tiv) * 2;
    }

    private record SovColumn(string Name, string Column, Type DataType);
}

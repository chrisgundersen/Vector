using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vector.Application.Common.Interfaces;
using Vector.Domain.DocumentProcessing.Enums;

namespace Vector.Infrastructure.DocumentAI;

/// <summary>
/// Azure Document Intelligence (Form Recognizer) implementation for document processing.
/// </summary>
public class AzureDocumentIntelligenceService : IDocumentIntelligenceService
{
    private readonly DocumentAnalysisClient _client;
    private readonly AzureDocumentIntelligenceOptions _options;
    private readonly ILogger<AzureDocumentIntelligenceService> _logger;

    private static readonly Dictionary<string, DocumentType> DocumentTypeMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["acord-125"] = DocumentType.Acord125,
        ["acord-126"] = DocumentType.Acord126,
        ["acord-130"] = DocumentType.Acord130,
        ["acord-140"] = DocumentType.Acord140,
        ["acord-127"] = DocumentType.Acord127,
        ["acord-137"] = DocumentType.Acord137,
        ["loss-run"] = DocumentType.LossRunReport,
        ["exposure-schedule"] = DocumentType.ExposureSchedule,
        ["sov"] = DocumentType.ExposureSchedule,
        ["policy-declaration"] = DocumentType.PolicyDeclaration,
        ["certificate"] = DocumentType.Certificate,
        ["endorsement"] = DocumentType.Endorsement
    };

    public AzureDocumentIntelligenceService(
        DocumentAnalysisClient client,
        IOptions<AzureDocumentIntelligenceOptions> options,
        ILogger<AzureDocumentIntelligenceService> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DocumentClassificationResult> ClassifyDocumentAsync(
        Stream documentStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        try
        {
            _logger.LogDebug("Classifying document {FileName}", fileName);

            // First, try to classify based on filename patterns
            var fileBasedClassification = ClassifyByFileName(fileName);
            if (fileBasedClassification.DocumentType != DocumentType.Unknown &&
                fileBasedClassification.Confidence >= 0.90m)
            {
                _logger.LogInformation(
                    "Document {FileName} classified as {DocumentType} by filename with confidence {Confidence:P}",
                    fileName,
                    fileBasedClassification.DocumentType,
                    fileBasedClassification.Confidence);
                return fileBasedClassification;
            }

            // Use Azure Document Intelligence for classification
            var classifierModelId = _options.ClassifierModelId;
            if (string.IsNullOrEmpty(classifierModelId))
            {
                // Fall back to prebuilt document analysis for content-based hints
                return await ClassifyWithPrebuiltModelAsync(documentStream, fileName, cancellationToken);
            }

            var operation = await _client.ClassifyDocumentAsync(
                WaitUntil.Completed,
                classifierModelId,
                documentStream,
                cancellationToken: cancellationToken);

            var result = operation.Value;

            if (result.Documents.Count == 0)
            {
                _logger.LogWarning("No document type detected for {FileName}", fileName);
                return new DocumentClassificationResult(DocumentType.Unknown, 0.30m, _options.ModelVersion);
            }

            var topDocument = result.Documents
                .OrderByDescending(d => d.Confidence)
                .First();

            var documentType = MapDocumentType(topDocument.DocumentType);
            var confidence = (decimal)topDocument.Confidence;

            _logger.LogInformation(
                "Document {FileName} classified as {DocumentType} with confidence {Confidence:P}",
                fileName,
                documentType,
                confidence);

            return new DocumentClassificationResult(documentType, confidence, _options.ModelVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying document {FileName}", fileName);
            throw;
        }
    }

    public async Task<DocumentExtractionResult> ExtractAcordFormAsync(
        Stream documentStream,
        DocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentStream);

        try
        {
            _logger.LogDebug("Extracting ACORD form type {DocumentType}", documentType);

            // Use custom model if configured, otherwise use prebuilt
            var modelId = GetModelIdForDocumentType(documentType);

            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                modelId,
                documentStream,
                cancellationToken: cancellationToken);

            var result = operation.Value;

            var fields = new Dictionary<string, ExtractedFieldResult>();
            var pageCount = result.Pages.Count;

            // Extract key-value pairs from analyzed document
            foreach (var kvp in result.KeyValuePairs)
            {
                if (kvp.Key?.Content is null) continue;

                var fieldName = NormalizeFieldName(kvp.Key.Content);
                var fieldValue = kvp.Value?.Content;
                var confidence = (decimal)kvp.Confidence;

                // Get page number from bounding region
                var firstRegion = kvp.Key.BoundingRegions.FirstOrDefault();
                int? pageNumber = firstRegion.PageNumber > 0 ? firstRegion.PageNumber : null;

                fields[fieldName] = new ExtractedFieldResult(
                    fieldValue,
                    confidence,
                    GetBoundingBoxString(firstRegion),
                    pageNumber);
            }

            // Also extract from document fields if using custom model
            if (result.Documents.Count > 0)
            {
                foreach (var doc in result.Documents)
                {
                    foreach (var (fieldName, field) in doc.Fields)
                    {
                        if (fields.ContainsKey(fieldName)) continue;

                        var fieldRegion = field.BoundingRegions.FirstOrDefault();
                        fields[fieldName] = new ExtractedFieldResult(
                            field.Content ?? field.Value?.ToString(),
                            (decimal)(field.Confidence ?? 0.5f),
                            GetBoundingBoxString(fieldRegion),
                            fieldRegion.PageNumber > 0 ? fieldRegion.PageNumber : null);
                    }
                }
            }

            // Map common ACORD field names
            MapAcordFieldNames(fields, documentType);

            var averageConfidence = fields.Count > 0
                ? fields.Values.Average(f => f.Confidence)
                : 0m;

            _logger.LogInformation(
                "Extracted {FieldCount} fields from {DocumentType} with average confidence {Confidence:P}",
                fields.Count,
                documentType,
                averageConfidence);

            return new DocumentExtractionResult(fields, averageConfidence, pageCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting ACORD form type {DocumentType}", documentType);
            throw;
        }
    }

    public async Task<LossRunExtractionResult> ExtractLossRunAsync(
        Stream documentStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentStream);

        try
        {
            _logger.LogDebug("Extracting loss run data");

            var modelId = _options.LossRunModelId ?? "prebuilt-document";

            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                modelId,
                documentStream,
                cancellationToken: cancellationToken);

            var result = operation.Value;

            var losses = new List<ExtractedLoss>();
            string? carrierName = null;
            DateTime? reportDate = null;
            var confidenceScores = new List<decimal>();

            // Extract from tables (loss runs typically are tabular)
            foreach (var table in result.Tables)
            {
                var columnMap = MapTableColumns(table);
                var headerRow = table.Cells.Where(c => c.RowIndex == 0).ToList();

                for (var rowIndex = 1; rowIndex < table.RowCount; rowIndex++)
                {
                    var rowCells = table.Cells.Where(c => c.RowIndex == rowIndex).ToDictionary(c => c.ColumnIndex);

                    var loss = ExtractLossFromRow(rowCells, columnMap);
                    if (loss is not null)
                    {
                        losses.Add(loss);
                        confidenceScores.Add(0.85m);
                    }
                }
            }

            // Try to extract carrier name and report date from key-value pairs
            foreach (var kvp in result.KeyValuePairs)
            {
                var keyLower = kvp.Key?.Content?.ToLowerInvariant() ?? string.Empty;

                if (keyLower.Contains("carrier") || keyLower.Contains("company") || keyLower.Contains("insurer"))
                {
                    carrierName = kvp.Value?.Content;
                }
                else if (keyLower.Contains("report date") || keyLower.Contains("as of") || keyLower.Contains("valuation"))
                {
                    if (DateTime.TryParse(kvp.Value?.Content, out var date))
                    {
                        reportDate = date;
                    }
                }
            }

            var averageConfidence = confidenceScores.Count > 0
                ? confidenceScores.Average()
                : 0.80m;

            _logger.LogInformation(
                "Extracted {LossCount} losses from loss run with average confidence {Confidence:P}",
                losses.Count,
                averageConfidence);

            return new LossRunExtractionResult(losses, carrierName, reportDate, averageConfidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting loss run data");
            throw;
        }
    }

    public async Task<ExposureScheduleExtractionResult> ExtractExposureScheduleAsync(
        Stream documentStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentStream);

        try
        {
            _logger.LogDebug("Extracting exposure schedule data");

            var modelId = _options.ExposureScheduleModelId ?? "prebuilt-document";

            var operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                modelId,
                documentStream,
                cancellationToken: cancellationToken);

            var result = operation.Value;

            var locations = new List<ExtractedLocation>();
            var confidenceScores = new List<decimal>();

            // Extract from tables (SOV/exposure schedules are typically tabular)
            foreach (var table in result.Tables)
            {
                var columnMap = MapTableColumnsForExposure(table);
                var headerRow = table.Cells.Where(c => c.RowIndex == 0).ToList();

                for (var rowIndex = 1; rowIndex < table.RowCount; rowIndex++)
                {
                    var rowCells = table.Cells.Where(c => c.RowIndex == rowIndex).ToDictionary(c => c.ColumnIndex);

                    var location = ExtractLocationFromRow(rowCells, columnMap, rowIndex);
                    if (location is not null)
                    {
                        locations.Add(location);
                        confidenceScores.Add(0.85m);
                    }
                }
            }

            var averageConfidence = confidenceScores.Count > 0
                ? confidenceScores.Average()
                : 0.80m;

            _logger.LogInformation(
                "Extracted {LocationCount} locations from exposure schedule with average confidence {Confidence:P}",
                locations.Count,
                averageConfidence);

            return new ExposureScheduleExtractionResult(locations, averageConfidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting exposure schedule data");
            throw;
        }
    }

    private DocumentClassificationResult ClassifyByFileName(string fileName)
    {
        var nameLower = fileName.ToLowerInvariant();

        if (nameLower.Contains("acord") && nameLower.Contains("125"))
            return new DocumentClassificationResult(DocumentType.Acord125, 0.95m, "filename-heuristic");

        if (nameLower.Contains("acord") && nameLower.Contains("126"))
            return new DocumentClassificationResult(DocumentType.Acord126, 0.95m, "filename-heuristic");

        if (nameLower.Contains("acord") && nameLower.Contains("130"))
            return new DocumentClassificationResult(DocumentType.Acord130, 0.95m, "filename-heuristic");

        if (nameLower.Contains("acord") && nameLower.Contains("140"))
            return new DocumentClassificationResult(DocumentType.Acord140, 0.95m, "filename-heuristic");

        if (nameLower.Contains("loss") && nameLower.Contains("run"))
            return new DocumentClassificationResult(DocumentType.LossRunReport, 0.92m, "filename-heuristic");

        if (nameLower.Contains("sov") || nameLower.Contains("schedule of values") ||
            nameLower.Contains("exposure") && nameLower.Contains("schedule"))
            return new DocumentClassificationResult(DocumentType.ExposureSchedule, 0.90m, "filename-heuristic");

        if (nameLower.Contains("declaration") || nameLower.Contains("dec page"))
            return new DocumentClassificationResult(DocumentType.PolicyDeclaration, 0.88m, "filename-heuristic");

        return new DocumentClassificationResult(DocumentType.Unknown, 0.30m, "filename-heuristic");
    }

    private async Task<DocumentClassificationResult> ClassifyWithPrebuiltModelAsync(
        Stream documentStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-document",
            documentStream,
            cancellationToken: cancellationToken);

        var result = operation.Value;

        // Analyze content to determine document type
        var contentLower = result.Content.ToLowerInvariant();

        if (contentLower.Contains("acord") && contentLower.Contains("commercial insurance application"))
            return new DocumentClassificationResult(DocumentType.Acord125, 0.85m, "prebuilt-content-analysis");

        if (contentLower.Contains("acord") && contentLower.Contains("general liability"))
            return new DocumentClassificationResult(DocumentType.Acord126, 0.85m, "prebuilt-content-analysis");

        if (contentLower.Contains("acord") && contentLower.Contains("workers compensation"))
            return new DocumentClassificationResult(DocumentType.Acord130, 0.85m, "prebuilt-content-analysis");

        if (contentLower.Contains("acord") && contentLower.Contains("property"))
            return new DocumentClassificationResult(DocumentType.Acord140, 0.85m, "prebuilt-content-analysis");

        if (contentLower.Contains("loss history") || contentLower.Contains("claim history") ||
            (contentLower.Contains("date of loss") && contentLower.Contains("paid")))
            return new DocumentClassificationResult(DocumentType.LossRunReport, 0.80m, "prebuilt-content-analysis");

        if (contentLower.Contains("schedule of values") || contentLower.Contains("location schedule") ||
            (contentLower.Contains("building value") && contentLower.Contains("address")))
            return new DocumentClassificationResult(DocumentType.ExposureSchedule, 0.80m, "prebuilt-content-analysis");

        // Check if it's likely an ACORD form based on content patterns
        if (contentLower.Contains("acord"))
            return new DocumentClassificationResult(DocumentType.Acord125, 0.70m, "prebuilt-content-analysis");

        return new DocumentClassificationResult(DocumentType.Unknown, 0.40m, "prebuilt-content-analysis");
    }

    private static DocumentType MapDocumentType(string typeString)
    {
        if (DocumentTypeMapping.TryGetValue(typeString, out var documentType))
        {
            return documentType;
        }

        return DocumentType.Unknown;
    }

    private string GetModelIdForDocumentType(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.Acord125 => _options.Acord125ModelId ?? "prebuilt-document",
            DocumentType.Acord126 => _options.Acord126ModelId ?? "prebuilt-document",
            DocumentType.Acord130 => _options.Acord130ModelId ?? "prebuilt-document",
            DocumentType.Acord140 => _options.Acord140ModelId ?? "prebuilt-document",
            _ => "prebuilt-document"
        };
    }

    private static string NormalizeFieldName(string fieldName)
    {
        // Remove common prefixes/suffixes and normalize to PascalCase
        var normalized = fieldName
            .Replace(":", "")
            .Replace(".", "")
            .Trim();

        // Convert to PascalCase
        var words = normalized.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w =>
            char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    private static void MapAcordFieldNames(Dictionary<string, ExtractedFieldResult> fields, DocumentType documentType)
    {
        // Map common variations to standard field names
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["named insured"] = "InsuredName",
            ["applicant name"] = "InsuredName",
            ["insured name"] = "InsuredName",
            ["applicant"] = "InsuredName",
            ["dba"] = "InsuredDBA",
            ["doing business as"] = "InsuredDBA",
            ["mailing address"] = "InsuredAddress",
            ["address"] = "InsuredAddress",
            ["city"] = "InsuredCity",
            ["state"] = "InsuredState",
            ["zip"] = "InsuredZip",
            ["zip code"] = "InsuredZip",
            ["postal code"] = "InsuredZip",
            ["fein"] = "FEIN",
            ["federal employer id"] = "FEIN",
            ["tax id"] = "FEIN",
            ["effective date"] = "EffectiveDate",
            ["policy effective"] = "EffectiveDate",
            ["expiration date"] = "ExpirationDate",
            ["policy expiration"] = "ExpirationDate",
            ["description of operations"] = "BusinessDescription",
            ["business description"] = "BusinessDescription",
            ["nature of business"] = "BusinessDescription",
            ["naics"] = "NAICSCode",
            ["naics code"] = "NAICSCode",
            ["sic"] = "SICCode",
            ["sic code"] = "SICCode",
            ["years in business"] = "YearsInBusiness",
            ["number of employees"] = "EmployeeCount",
            ["employees"] = "EmployeeCount",
            ["annual revenue"] = "AnnualRevenue",
            ["gross receipts"] = "AnnualRevenue"
        };

        var keysToRemove = new List<string>();
        var fieldsToAdd = new Dictionary<string, ExtractedFieldResult>();

        foreach (var (key, value) in fields)
        {
            if (mappings.TryGetValue(key, out var standardName) && !fields.ContainsKey(standardName))
            {
                fieldsToAdd[standardName] = value;
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            fields.Remove(key);
        }

        foreach (var (key, value) in fieldsToAdd)
        {
            fields[key] = value;
        }
    }

    private static string? GetBoundingBoxString(BoundingRegion region)
    {
        if (region.BoundingPolygon.Count == 0)
            return null;

        var points = region.BoundingPolygon.Select(p => $"{p.X:F2},{p.Y:F2}");
        return string.Join(";", points);
    }

    private static Dictionary<int, string> MapTableColumns(DocumentTable table)
    {
        var columnMap = new Dictionary<int, string>();
        var headerCells = table.Cells.Where(c => c.RowIndex == 0).ToList();

        foreach (var cell in headerCells)
        {
            var header = cell.Content?.ToLowerInvariant() ?? string.Empty;
            columnMap[cell.ColumnIndex] = header;
        }

        return columnMap;
    }

    private static Dictionary<int, string> MapTableColumnsForExposure(DocumentTable table)
    {
        return MapTableColumns(table);
    }

    private static ExtractedLoss? ExtractLossFromRow(
        Dictionary<int, DocumentTableCell> rowCells,
        Dictionary<int, string> columnMap)
    {
        DateTime? dateOfLoss = null;
        string? claimNumber = null;
        string? description = null;
        decimal? paidAmount = null;
        decimal? reservedAmount = null;
        decimal? incurredAmount = null;
        string? status = null;
        string? coverageType = null;

        foreach (var (colIndex, header) in columnMap)
        {
            if (!rowCells.TryGetValue(colIndex, out var cell)) continue;

            var value = cell.Content?.Trim();
            if (string.IsNullOrEmpty(value)) continue;

            if (header.Contains("date") && header.Contains("loss"))
            {
                if (DateTime.TryParse(value, out var date))
                    dateOfLoss = date;
            }
            else if (header.Contains("claim") && header.Contains("number"))
            {
                claimNumber = value;
            }
            else if (header.Contains("description") || header.Contains("cause"))
            {
                description = value;
            }
            else if (header.Contains("paid"))
            {
                if (TryParseDecimal(value, out var amount))
                    paidAmount = amount;
            }
            else if (header.Contains("reserve"))
            {
                if (TryParseDecimal(value, out var amount))
                    reservedAmount = amount;
            }
            else if (header.Contains("incurred") || header.Contains("total"))
            {
                if (TryParseDecimal(value, out var amount))
                    incurredAmount = amount;
            }
            else if (header.Contains("status"))
            {
                status = value;
            }
            else if (header.Contains("coverage") || header.Contains("type"))
            {
                coverageType = value;
            }
        }

        // Only return if we have meaningful data
        if (dateOfLoss is null && claimNumber is null && paidAmount is null)
            return null;

        return new ExtractedLoss(
            dateOfLoss,
            claimNumber,
            description,
            paidAmount,
            reservedAmount,
            incurredAmount ?? (paidAmount ?? 0) + (reservedAmount ?? 0),
            status,
            coverageType);
    }

    private static ExtractedLocation? ExtractLocationFromRow(
        Dictionary<int, DocumentTableCell> rowCells,
        Dictionary<int, string> columnMap,
        int rowIndex)
    {
        int? locationNumber = null;
        string? street1 = null;
        string? street2 = null;
        string? city = null;
        string? state = null;
        string? postalCode = null;
        string? buildingDescription = null;
        decimal? buildingValue = null;
        decimal? contentsValue = null;
        decimal? businessIncomeValue = null;
        string? constructionType = null;
        int? yearBuilt = null;
        int? squareFootage = null;

        foreach (var (colIndex, header) in columnMap)
        {
            if (!rowCells.TryGetValue(colIndex, out var cell)) continue;

            var value = cell.Content?.Trim();
            if (string.IsNullOrEmpty(value)) continue;

            if (header.Contains("loc") && (header.Contains("#") || header.Contains("num")))
            {
                if (int.TryParse(value, out var locNum))
                    locationNumber = locNum;
            }
            else if (header.Contains("address") || header.Contains("street"))
            {
                street1 ??= value;
            }
            else if (header.Contains("city"))
            {
                city = value;
            }
            else if (header.Contains("state") || header.Contains("st"))
            {
                state = value;
            }
            else if (header.Contains("zip") || header.Contains("postal"))
            {
                postalCode = value;
            }
            else if (header.Contains("building") && header.Contains("value"))
            {
                if (TryParseDecimal(value, out var amount))
                    buildingValue = amount;
            }
            else if (header.Contains("contents") || header.Contains("bpp"))
            {
                if (TryParseDecimal(value, out var amount))
                    contentsValue = amount;
            }
            else if (header.Contains("bi") || header.Contains("business income"))
            {
                if (TryParseDecimal(value, out var amount))
                    businessIncomeValue = amount;
            }
            else if (header.Contains("construction"))
            {
                constructionType = value;
            }
            else if (header.Contains("year") && header.Contains("built"))
            {
                if (int.TryParse(value, out var year))
                    yearBuilt = year;
            }
            else if (header.Contains("sq") || header.Contains("footage") || header.Contains("area"))
            {
                if (int.TryParse(value.Replace(",", ""), out var sqft))
                    squareFootage = sqft;
            }
            else if (header.Contains("description"))
            {
                buildingDescription = value;
            }
        }

        // Only return if we have meaningful data
        if (street1 is null && buildingValue is null)
            return null;

        return new ExtractedLocation(
            locationNumber ?? rowIndex,
            street1,
            street2,
            city,
            state,
            postalCode,
            buildingDescription,
            buildingValue,
            contentsValue,
            businessIncomeValue,
            constructionType,
            yearBuilt,
            squareFootage);
    }

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;

        // Remove currency symbols and common formatting
        var cleaned = value
            .Replace("$", "")
            .Replace(",", "")
            .Replace(" ", "")
            .Trim();

        return decimal.TryParse(cleaned, out result);
    }
}

/// <summary>
/// Configuration options for Azure Document Intelligence service.
/// </summary>
public class AzureDocumentIntelligenceOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "DocumentIntelligence:Azure";

    /// <summary>
    /// Azure Document Intelligence endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure Document Intelligence API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Custom classifier model ID for document classification.
    /// </summary>
    public string? ClassifierModelId { get; set; }

    /// <summary>
    /// Custom model ID for ACORD 125 extraction.
    /// </summary>
    public string? Acord125ModelId { get; set; }

    /// <summary>
    /// Custom model ID for ACORD 126 extraction.
    /// </summary>
    public string? Acord126ModelId { get; set; }

    /// <summary>
    /// Custom model ID for ACORD 130 extraction.
    /// </summary>
    public string? Acord130ModelId { get; set; }

    /// <summary>
    /// Custom model ID for ACORD 140 extraction.
    /// </summary>
    public string? Acord140ModelId { get; set; }

    /// <summary>
    /// Custom model ID for loss run extraction.
    /// </summary>
    public string? LossRunModelId { get; set; }

    /// <summary>
    /// Custom model ID for exposure schedule extraction.
    /// </summary>
    public string? ExposureScheduleModelId { get; set; }

    /// <summary>
    /// Model version for tracking.
    /// </summary>
    public string ModelVersion { get; set; } = "1.0";
}

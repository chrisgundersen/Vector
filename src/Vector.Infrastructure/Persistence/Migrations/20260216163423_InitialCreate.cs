using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataCorrectionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CurrentValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProposedValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCorrectionRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboundEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MailboxId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FromAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    Subject = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BodyPreview = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ContentHashAlgorithm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessingError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundEmails", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InboundEmailsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ProcessingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InboundEmailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingJobs", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ProcessingJobsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ProducerUnderwriterPairings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProducerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProducerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UnderwriterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnderwriterName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CoverageTypes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProducerUnderwriterPairings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutingDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Strategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedUnderwriterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedUnderwriterName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AssignedTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedTeamName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MatchedRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchedRuleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MatchedPairingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoutingReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AppetiteScore = table.Column<int>(type: "int", nullable: true),
                    WinnabilityScore = table.Column<int>(type: "int", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeclinedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeclineReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    History = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingDecisions", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoutingDecisionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "RoutingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Strategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetUnderwriterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetUnderwriterName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TargetTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetTeamName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    Conditions = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingRules", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoutingRulesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InboundEmailId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InsuredName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    InsuredDbaName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InsuredFein = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InsuredStreet1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InsuredStreet2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InsuredCity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InsuredState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InsuredPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InsuredCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    InsuredNaicsCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    InsuredSicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    InsuredIndustryDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InsuredWebsite = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InsuredYearsInBusiness = table.Column<int>(type: "int", nullable: true),
                    InsuredEmployeeCount = table.Column<int>(type: "int", nullable: true),
                    InsuredAnnualRevenueAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InsuredAnnualRevenueCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    InsuredEntityFormationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InsuredEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InsuredId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProducerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProducerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProducerContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AssignedUnderwriterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedUnderwriterName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppetiteScore = table.Column<int>(type: "int", nullable: true),
                    WinnabilityScore = table.Column<int>(type: "int", nullable: true),
                    DataQualityScore = table.Column<int>(type: "int", nullable: true),
                    DeclineReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    QuotedPremiumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuotedPremiumCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ClearanceStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClearanceCheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClearanceOverrideReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClearanceOverriddenByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClearanceOverriddenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SubmissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "UnderwritingGuidelines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ApplicableCoverageTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApplicableStates = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApplicableNAICSCodes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnderwritingGuidelines", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UnderwritingGuidelinesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "EmailAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ContentHashAlgorithm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    BlobStorageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExtractedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InboundEmailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAttachments_InboundEmails_InboundEmailId",
                        column: x => x.InboundEmailId,
                        principalTable: "InboundEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EmailAttachmentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ProcessedDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BlobStorageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClassificationConfidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidationErrors = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessedDocuments_ProcessingJobs_ProcessingJobId",
                        column: x => x.ProcessingJobId,
                        principalTable: "ProcessingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ProcessedDocumentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "ClearanceMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchedSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchedSubmissionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatchType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: false),
                    MatchDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearanceMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClearanceMatches_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClearanceMatchesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "SubmissionCoverages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedLimitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RequestedLimitCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    RequestedDeductibleAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RequestedDeductibleCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrentlyInsured = table.Column<bool>(type: "bit", nullable: false),
                    CurrentCarrier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentPremiumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrentPremiumCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionCoverages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionCoverages_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SubmissionCoveragesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "SubmissionLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationNumber = table.Column<int>(type: "int", nullable: false),
                    Street1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Street2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    BuildingDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccupancyType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConstructionType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    YearBuilt = table.Column<int>(type: "int", nullable: true),
                    SquareFootage = table.Column<int>(type: "int", nullable: true),
                    NumberOfStories = table.Column<int>(type: "int", nullable: true),
                    BuildingValueAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BuildingValueCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ContentsValueAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ContentsValueCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    BusinessIncomeValueAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BusinessIncomeValueCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    HasSprinklers = table.Column<bool>(type: "bit", nullable: false),
                    HasFireAlarm = table.Column<bool>(type: "bit", nullable: false),
                    HasSecuritySystem = table.Column<bool>(type: "bit", nullable: false),
                    ProtectionClass = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionLocations_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SubmissionLocationsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "SubmissionLossHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateOfLoss = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CoverageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClaimNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaidCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ReservedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ReservedCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    IncurredAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IncurredCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Carrier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsSubrogation = table.Column<bool>(type: "bit", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionLossHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionLossHistory_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SubmissionLossHistoryHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "UnderwritingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ScoreAdjustment = table.Column<int>(type: "int", nullable: true),
                    PricingModifier = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GuidelineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Conditions = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnderwritingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnderwritingRules_UnderwritingGuidelines_GuidelineId",
                        column: x => x.GuidelineId,
                        principalTable: "UnderwritingGuidelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtractedFields",
                columns: table => new
                {
                    ProcessedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    BoundingBox = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PageNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedFields", x => new { x.ProcessedDocumentId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExtractedFields_ProcessedDocuments_ProcessedDocumentId",
                        column: x => x.ProcessedDocumentId,
                        principalTable: "ProcessedDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClearanceMatches_MatchedSubmissionId",
                table: "ClearanceMatches",
                column: "MatchedSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClearanceMatches_SubmissionId",
                table: "ClearanceMatches",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_Status",
                table: "DataCorrectionRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_SubmissionId",
                table: "DataCorrectionRequests",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_DataCorrectionRequests_SubmissionId_Status",
                table: "DataCorrectionRequests",
                columns: new[] { "SubmissionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_InboundEmailId",
                table: "EmailAttachments",
                column: "InboundEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundEmails_MailboxId",
                table: "InboundEmails",
                column: "MailboxId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundEmails_ReceivedAt",
                table: "InboundEmails",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InboundEmails_Status",
                table: "InboundEmails",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InboundEmails_TenantId",
                table: "InboundEmails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundEmails_TenantId_ExternalMessageId",
                table: "InboundEmails",
                columns: new[] { "TenantId", "ExternalMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedDocuments_ProcessingJobId",
                table: "ProcessedDocuments",
                column: "ProcessingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_InboundEmailId",
                table: "ProcessingJobs",
                column: "InboundEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_StartedAt",
                table: "ProcessingJobs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_Status",
                table: "ProcessingJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_TenantId",
                table: "ProcessingJobs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProducerUnderwriterPairings_ProducerId",
                table: "ProducerUnderwriterPairings",
                column: "ProducerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProducerUnderwriterPairings_ProducerId_IsActive",
                table: "ProducerUnderwriterPairings",
                columns: new[] { "ProducerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProducerUnderwriterPairings_UnderwriterId",
                table: "ProducerUnderwriterPairings",
                column: "UnderwriterId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingDecisions_AssignedUnderwriterId",
                table: "RoutingDecisions",
                column: "AssignedUnderwriterId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingDecisions_Status",
                table: "RoutingDecisions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingDecisions_SubmissionId",
                table: "RoutingDecisions",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutingRules_Priority",
                table: "RoutingRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingRules_Status",
                table: "RoutingRules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingRules_Strategy",
                table: "RoutingRules",
                column: "Strategy");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionCoverages_SubmissionId",
                table: "SubmissionCoverages",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionLocations_SubmissionId",
                table: "SubmissionLocations",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionLossHistory_SubmissionId",
                table: "SubmissionLossHistory",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignedUnderwriterId",
                table: "Submissions",
                column: "AssignedUnderwriterId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ClearanceStatus",
                table: "Submissions",
                column: "ClearanceStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ProcessingJobId",
                table: "Submissions",
                column: "ProcessingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ProducerId",
                table: "Submissions",
                column: "ProducerId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ReceivedAt",
                table: "Submissions",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_Status",
                table: "Submissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TenantId",
                table: "Submissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TenantId_SubmissionNumber",
                table: "Submissions",
                columns: new[] { "TenantId", "SubmissionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnderwritingGuidelines_Status",
                table: "UnderwritingGuidelines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UnderwritingGuidelines_TenantId",
                table: "UnderwritingGuidelines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UnderwritingGuidelines_TenantId_Status",
                table: "UnderwritingGuidelines",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UnderwritingRules_GuidelineId",
                table: "UnderwritingRules",
                column: "GuidelineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClearanceMatches")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClearanceMatchesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "DataCorrectionRequests");

            migrationBuilder.DropTable(
                name: "EmailAttachments")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "EmailAttachmentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "ExtractedFields");

            migrationBuilder.DropTable(
                name: "ProducerUnderwriterPairings");

            migrationBuilder.DropTable(
                name: "RoutingDecisions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoutingDecisionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "RoutingRules")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoutingRulesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "SubmissionCoverages")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SubmissionCoveragesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "SubmissionLocations")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SubmissionLocationsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "SubmissionLossHistory")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SubmissionLossHistoryHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "UnderwritingRules");

            migrationBuilder.DropTable(
                name: "InboundEmails")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InboundEmailsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "ProcessedDocuments")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ProcessedDocumentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "Submissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SubmissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "UnderwritingGuidelines")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UnderwritingGuidelinesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "ProcessingJobs")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ProcessingJobsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}

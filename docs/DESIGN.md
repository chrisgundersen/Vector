# Vector System Design Document

## Document Information

| Attribute | Value |
|-----------|-------|
| Version | 1.0.0 |
| Status | Active |
| Last Updated | 2024-01 |
| Authors | Vector Development Team |

---

## Table of Contents

1. [Introduction](#introduction)
2. [System Overview](#system-overview)
3. [Design Goals](#design-goals)
4. [Domain Model](#domain-model)
5. [Application Architecture](#application-architecture)
6. [Data Architecture](#data-architecture)
7. [Integration Design](#integration-design)
8. [Security Design](#security-design)
9. [Performance Design](#performance-design)
10. [Testing Strategy](#testing-strategy)
11. [Deployment Design](#deployment-design)

---

## Introduction

### Purpose

This document describes the technical design of the Vector insurance submission management system. It provides comprehensive documentation for developers, architects, and operations teams.

### Scope

Vector handles the complete lifecycle of insurance submissions:
- Email ingestion from shared mailboxes
- Document classification and data extraction
- Submission creation and enrichment
- Underwriting workflow management
- Integration with external systems

### Audience

- Software developers implementing features
- Solution architects making design decisions
- DevOps engineers deploying and operating the system
- Security teams reviewing the implementation

---

## System Overview

### Context

Vector operates within an insurance ecosystem:

```
┌──────────────────────────────────────────────────────────────────┐
│                    Insurance Ecosystem                            │
│                                                                   │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐          │
│  │  Producers  │───>│   Vector    │───>│ Underwriters│          │
│  │  (Brokers)  │    │   System    │    │             │          │
│  └─────────────┘    └──────┬──────┘    └─────────────┘          │
│                            │                                      │
│                    ┌───────┴───────┐                             │
│                    │               │                             │
│              ┌─────┴─────┐   ┌─────┴─────┐                      │
│              │    PAS    │   │    CRM    │                      │
│              │  (Policy) │   │ (Customer)│                      │
│              └───────────┘   └───────────┘                      │
└──────────────────────────────────────────────────────────────────┘
```

### Key Capabilities

| Capability | Description |
|------------|-------------|
| **Email Intake** | Automated monitoring of shared mailboxes via Microsoft Graph |
| **Document AI** | Classification and extraction using Azure Document Intelligence |
| **Routing** | Rule-based assignment to underwriters |
| **Scoring** | Appetite, winnability, and data quality scoring |
| **Workflow** | Quote, decline, bind lifecycle management |

---

## Design Goals

### Functional Goals

1. **Automation**: Achieve 80% straight-through processing
2. **Accuracy**: > 95% extraction accuracy on standard forms
3. **Speed**: < 30 second email-to-submission processing
4. **Scalability**: Handle 5,000+ submissions/day

### Non-Functional Goals

| Goal | Target | Measurement |
|------|--------|-------------|
| Availability | 99.9% | Uptime monitoring |
| Latency | < 500ms p95 | API response time |
| Throughput | 100 req/sec | Load testing |
| Recovery | < 4 hours RTO | Disaster recovery testing |

### Design Principles

1. **Domain-Driven Design**: Business logic in domain layer
2. **CQRS**: Separate read/write models
3. **Event-Driven**: Loose coupling via domain events
4. **API-First**: Versioned REST APIs
5. **Cloud-Native**: Container-based, horizontally scalable

---

## Domain Model

### Bounded Contexts

```
┌─────────────────────────────────────────────────────────────────┐
│                      Vector Domain                               │
├─────────────┬─────────────┬─────────────┬─────────────┬─────────┤
│   Email     │  Document   │ Submission  │ Underwriting│ Routing │
│   Intake    │ Processing  │             │ Guidelines  │         │
├─────────────┼─────────────┼─────────────┼─────────────┼─────────┤
│ InboundEmail│ProcessingJob│ Submission  │ Guideline   │RoutingRule
│ Attachment  │ Document    │ Coverage    │ Rule        │ Pairing │
│             │ Field       │ Location    │ Criterion   │ Decision│
│             │             │ LossHistory │             │         │
└─────────────┴─────────────┴─────────────┴─────────────┴─────────┘
```

### Aggregate Design

#### Submission Aggregate

The core aggregate managing the submission lifecycle:

```csharp
public class Submission : AggregateRoot
{
    public Guid TenantId { get; }
    public string SubmissionNumber { get; }
    public SubmissionStatus Status { get; private set; }
    public InsuredParty Insured { get; private set; }

    private readonly List<Coverage> _coverages = [];
    private readonly List<ExposureLocation> _locations = [];
    private readonly List<LossHistoryItem> _lossHistory = [];

    // Lifecycle methods
    public void MarkAsReceived();
    public void AssignToUnderwriter(Guid underwriterId, string name);
    public void Quote(Money premium);
    public void Decline(string reason);
    public void Bind();
    public void RequestInformation(string message);
}
```

**Invariants:**
- Submission number must be unique within tenant
- Cannot quote without assigned underwriter
- Cannot bind without quote
- Cannot perform actions on declined/bound submissions

#### Processing Job Aggregate

Manages document processing workflow:

```csharp
public class ProcessingJob : AggregateRoot
{
    public Guid TenantId { get; }
    public ProcessingStatus Status { get; private set; }

    private readonly List<ProcessedDocument> _documents = [];

    public void StartProcessing();
    public void AddDocument(ProcessedDocument document);
    public void Complete(int dataQualityScore);
    public void Fail(string reason);
}
```

### Domain Events

| Event | Publisher | Consumers |
|-------|-----------|-----------|
| `EmailReceivedEvent` | EmailIntake | DocumentProcessing |
| `DocumentProcessedEvent` | DocumentProcessing | Submission |
| `SubmissionCreatedEvent` | Submission | Routing, Scoring |
| `SubmissionAssignedEvent` | Submission | Notification |
| `SubmissionQuotedEvent` | Submission | Notification |
| `SubmissionBoundEvent` | Submission | PAS Integration |

### Value Objects

```csharp
public record Money(decimal Amount, string Currency)
{
    public Money Add(Money other) =>
        new(Amount + other.Amount, Currency);
}

public record Address(
    string Street1, string? Street2,
    string City, string State,
    string PostalCode, string Country);

public record InsuredParty(
    string Name, string? DBA,
    string? TaxId, int? YearsInBusiness);
```

---

## Application Architecture

### Layer Responsibilities

| Layer | Responsibility | Dependencies |
|-------|----------------|--------------|
| **API** | HTTP endpoints, authentication | Application |
| **Application** | Commands, queries, orchestration | Domain |
| **Domain** | Business logic, aggregates | None |
| **Infrastructure** | Data access, external services | Domain |

### CQRS Implementation

#### Commands

```csharp
public record QuoteSubmissionCommand(
    Guid SubmissionId,
    decimal PremiumAmount,
    string Currency) : IRequest<Result<QuoteResult>>;

public class QuoteSubmissionCommandHandler : IRequestHandler<QuoteSubmissionCommand, Result<QuoteResult>>
{
    public async Task<Result<QuoteResult>> Handle(
        QuoteSubmissionCommand request,
        CancellationToken ct)
    {
        var submission = await _repository.GetByIdAsync(request.SubmissionId, ct);
        if (submission is null)
            return Result.Failure<QuoteResult>(SubmissionErrors.NotFound);

        var premium = Money.FromDecimal(request.PremiumAmount, request.Currency);
        submission.Quote(premium);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new QuoteResult(submission.Id, premium));
    }
}
```

#### Queries

```csharp
public record GetSubmissionQuery(Guid SubmissionId) : IRequest<SubmissionDto?>;

public class GetSubmissionQueryHandler : IRequestHandler<GetSubmissionQuery, SubmissionDto?>
{
    public async Task<SubmissionDto?> Handle(
        GetSubmissionQuery request,
        CancellationToken ct)
    {
        var submission = await _repository.GetByIdAsync(request.SubmissionId, ct);
        return submission?.ToDto();
    }
}
```

### MediatR Pipeline

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssembly).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
});
```

---

## Data Architecture

### Database Schema

#### Core Tables

```sql
CREATE TABLE Submissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    SubmissionNumber NVARCHAR(50) NOT NULL,
    Status INT NOT NULL,
    InsuredName NVARCHAR(500) NOT NULL,
    InsuredDBA NVARCHAR(500),
    ProducerId UNIQUEIDENTIFIER,
    AssignedUnderwriterId UNIQUEIDENTIFIER,
    QuotedPremiumAmount DECIMAL(18,2),
    QuotedPremiumCurrency CHAR(3),
    AppetiteScore INT,
    WinnabilityScore INT,
    DataQualityScore INT,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,

    INDEX IX_Submissions_TenantId_Status (TenantId, Status),
    INDEX IX_Submissions_SubmissionNumber (SubmissionNumber)
);

-- Temporal table for audit
ALTER TABLE Submissions
ADD PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);

ALTER TABLE Submissions
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.SubmissionsHistory));
```

### Multi-Tenancy

Tenant isolation via EF Core global query filters:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Submission>()
        .HasQueryFilter(s => s.TenantId == _tenantId);
}
```

### Caching Strategy

| Data | Cache Location | TTL | Invalidation |
|------|---------------|-----|--------------|
| Submission details | Redis | 5 min | On update |
| Guidelines | Redis | 15 min | On update |
| Routing rules | Redis | 15 min | On update |
| User sessions | Redis | 30 min | On logout |

---

## Integration Design

### Email Integration (Microsoft Graph)

```csharp
public interface IEmailService
{
    Task<IReadOnlyList<EmailMessage>> GetNewEmailsAsync(
        string mailbox, int maxResults, CancellationToken ct);
    Task<IReadOnlyList<EmailAttachment>> GetAttachmentsAsync(
        string mailbox, string messageId, CancellationToken ct);
    Task MoveToProcessedAsync(
        string mailbox, string messageId, string folder, CancellationToken ct);
}
```

**Deduplication Strategy:**
1. Calculate SHA-256 hash of email content
2. Check Redis for existing hash
3. If new, process and store hash with 30-day TTL

### Document Intelligence Integration

```csharp
public interface IDocumentIntelligenceService
{
    Task<DocumentClassificationResult> ClassifyDocumentAsync(
        Stream document, CancellationToken ct);
    Task<DocumentExtractionResult> ExtractDataAsync(
        Stream document, string modelId, CancellationToken ct);
}
```

**Supported Document Types:**
- ACORD 125 (Commercial Insurance Application)
- ACORD 126 (Commercial General Liability)
- ACORD 130 (Workers Compensation)
- ACORD 140 (Property)
- Loss Run Reports
- Statement of Values (SOV)

### External System Integrations

```csharp
// Abstraction for future PAS integration
public interface IExternalPolicyService
{
    Task<PolicyCreationResult> CreatePolicyAsync(
        Submission submission, CancellationToken ct);
}

// NoOp implementation for MVP
public class NoOpPolicyService : IExternalPolicyService
{
    public Task<PolicyCreationResult> CreatePolicyAsync(
        Submission submission, CancellationToken ct)
    {
        var policyNumber = $"POL-{submission.SubmissionNumber}";
        return Task.FromResult(new PolicyCreationResult(policyNumber, true));
    }
}
```

---

## Security Design

### Authentication Flow

```
┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐
│ Client  │───>│ Entra ID│───>│ API     │───>│ Resource│
│         │<───│         │<───│         │<───│         │
└─────────┘    └─────────┘    └─────────┘    └─────────┘
    │              │              │              │
    │   1. Login   │              │              │
    │─────────────>│              │              │
    │              │              │              │
    │   2. Token   │              │              │
    │<─────────────│              │              │
    │              │              │              │
    │   3. Request + Bearer Token │              │
    │────────────────────────────>│              │
    │              │              │              │
    │              │  4. Validate │              │
    │              │<─────────────│              │
    │              │              │              │
    │              │  5. Claims   │              │
    │              │─────────────>│              │
    │              │              │              │
    │              │              │  6. Authorize│
    │              │              │─────────────>│
```

### Authorization Model

| Role | Permissions |
|------|-------------|
| Producer | View own submissions, submit data corrections |
| Underwriter | View assigned submissions, quote/decline/bind |
| Admin | Configure guidelines, routing rules, all submissions |

### Security Controls

| Control | Implementation |
|---------|----------------|
| Authentication | Microsoft Entra ID + JWT |
| Authorization | RBAC + resource-based policies |
| Encryption (Transit) | TLS 1.3 |
| Encryption (Rest) | Azure SQL TDE, Blob encryption |
| Secrets | Azure Key Vault |
| Audit | SQL temporal tables + event log |

---

## Performance Design

### Scalability

| Component | Scaling Strategy |
|-----------|------------------|
| API | Horizontal (HPA based on CPU/memory) |
| Worker | Horizontal (HPA based on queue depth) |
| Database | Vertical + read replicas |
| Cache | Redis cluster |

### Performance Optimizations

1. **Query Optimization**
   - Proper indexing on frequently queried columns
   - `AsNoTracking()` for read-only queries
   - Projection to DTOs

2. **Caching**
   - Redis for frequently accessed data
   - In-memory cache for reference data

3. **Async Processing**
   - All I/O operations are async
   - Background processing via Service Bus

4. **Connection Pooling**
   - SQL connection pooling via EF Core
   - Redis connection multiplexing

---

## Testing Strategy

### Test Pyramid

```
        ╱╲
       ╱  ╲
      ╱ E2E╲        5% - End-to-end tests
     ╱──────╲
    ╱Integration╲   20% - Integration tests
   ╱────────────╲
  ╱    Unit      ╲  75% - Unit tests
 ╱────────────────╲
```

### Test Organization

| Type | Location | Focus |
|------|----------|-------|
| Unit | `tests/*.UnitTests/` | Domain, Application logic |
| Integration | `tests/*.IntegrationTests/` | Infrastructure, API |
| E2E | `tests/*.E2ETests/` | Full workflows |

### Test Naming Convention

```csharp
// Pattern: MethodName_Condition_ExpectedResult
[Fact]
public async Task Quote_WithValidPremium_UpdatesSubmissionStatus()
{
    // Arrange
    // Act
    // Assert
}
```

---

## Deployment Design

### Environment Progression

```
Development → Test → Staging → Production
    │          │        │          │
 Feature    Automated  Pre-prod   Live
 branches    testing   validation traffic
```

### Kubernetes Resources

```yaml
# Deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: vector-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: vector-api
  template:
    spec:
      containers:
      - name: api
        image: vector-api:latest
        ports:
        - containerPort: 8080
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
```

### CI/CD Pipeline

1. **Build**: Compile, run unit tests
2. **Scan**: Security vulnerability scan
3. **Package**: Build Docker images
4. **Deploy Staging**: Automatic deployment
5. **Integration Tests**: Run against staging
6. **Deploy Production**: Manual approval

---

## Appendices

### A. Glossary

| Term | Definition |
|------|------------|
| ACORD | Association for Cooperative Operations Research and Development - insurance form standards |
| MGU | Managing General Underwriter |
| SOV | Statement of Values - property schedule |
| STP | Straight-Through Processing |

### B. Related Documents

- [Architecture Overview](./architecture/ARCHITECTURE.md)
- [Operations Guide](./operations/OPERATIONS.md)
- [API Reference](../README.md)

### C. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0.0 | 2024-01 | Development Team | Initial version |

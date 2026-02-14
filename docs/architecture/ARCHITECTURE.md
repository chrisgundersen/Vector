# Vector System Architecture

## Overview

Vector is an enterprise-grade insurance submission management system built on modern cloud-native principles. This document provides comprehensive architectural documentation following ArchiMate 3.2 notation.

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Architectural Principles](#architectural-principles)
3. [Business Architecture](#business-architecture)
4. [Application Architecture](#application-architecture)
5. [Technology Architecture](#technology-architecture)
6. [Data Architecture](#data-architecture)
7. [Integration Architecture](#integration-architecture)
8. [Security Architecture](#security-architecture)
9. [Deployment Architecture](#deployment-architecture)

---

## Executive Summary

Vector automates the insurance submission intake process for MGUs (Managing General Underwriters) and wholesale brokers. The system:

- **Ingests** emails and documents from shared mailboxes
- **Parses** ACORD forms, loss runs, and exposure schedules using AI
- **Routes** submissions to underwriters based on configurable rules
- **Scores** submissions for appetite, winnability, and data quality
- **Manages** the full submission lifecycle through to binding

### Key Metrics
- **Target Volume**: 5,000+ submissions/day, 75,000+ documents/day
- **Response Time**: < 500ms API response (p95)
- **Availability**: 99.9% uptime SLA
- **Processing Time**: < 30 seconds email-to-submission

---

## Architectural Principles

| Principle | Description | Rationale |
|-----------|-------------|-----------|
| **Domain-Driven Design** | Bounded contexts with rich domain models | Aligns code with business concepts |
| **CQRS** | Separate command and query responsibilities | Optimizes read/write patterns |
| **Event-Driven** | Domain events for cross-context communication | Loose coupling between modules |
| **Cloud-Native** | Containerized, horizontally scalable | Elastic scaling, resilience |
| **API-First** | RESTful APIs with OpenAPI specs | Integration flexibility |
| **Multi-Tenant** | Tenant isolation at data layer | Single deployment, multiple customers |

---

## Business Architecture

### Business Capabilities

```
Insurance Submission Management
├── Submission Intake
│   ├── Email Monitoring
│   ├── Document Extraction
│   └── Data Capture
├── Document Processing
│   ├── Classification
│   ├── Data Extraction
│   └── Validation
├── Underwriting Support
│   ├── Guideline Management
│   ├── Appetite Scoring
│   └── Risk Assessment
├── Submission Routing
│   ├── Rule-Based Assignment
│   ├── Workload Balancing
│   └── Producer Pairing
└── Submission Lifecycle
    ├── Quote Management
    ├── Bind Processing
    └── Decline Handling
```

### Business Processes

#### BP-001: Email-to-Submission Process
1. **Trigger**: Email arrives in shared mailbox
2. **Email Intake**: System polls mailbox, extracts email and attachments
3. **Deduplication**: Content hash checked against cache
4. **Document Storage**: Attachments uploaded to blob storage
5. **Processing Job**: Created and queued for document processing
6. **Classification**: AI classifies document types
7. **Extraction**: Data extracted from ACORD forms, loss runs, SOVs
8. **Submission Creation**: New submission created from extracted data
9. **Scoring**: Appetite, winnability, data quality scores calculated
10. **Routing**: Submission routed to appropriate underwriter
11. **Assignment**: Underwriter notified of new submission

### Business Actors

| Actor | Description | Key Interactions |
|-------|-------------|------------------|
| **Producer** | Insurance broker/agent submitting business | Submit documents, track status, correct data |
| **Underwriter** | Reviews and processes submissions | Review queue, quote/decline, request info |
| **MGU Administrator** | Configures system settings | Manage guidelines, routing rules, pairings |
| **System Administrator** | Manages technical operations | Monitor health, manage deployments |

---

## Application Architecture

### Bounded Contexts

The system is organized into six bounded contexts, each with clear responsibilities:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Vector System                             │
├─────────────┬─────────────┬─────────────┬─────────────┬─────────┤
│ Email       │ Document    │ Submission  │ Underwriting│ Routing │
│ Intake      │ Processing  │ Management  │ Guidelines  │         │
├─────────────┼─────────────┼─────────────┼─────────────┼─────────┤
│ InboundEmail│ ProcessingJob│ Submission  │ Guideline   │RoutingRule│
│ Attachment  │ Document    │ Coverage    │ Rule        │ Pairing │
│             │ ExtractedData│ Location   │ Criterion   │ Decision│
│             │             │ LossHistory │             │         │
└─────────────┴─────────────┴─────────────┴─────────────┴─────────┘
```

### Context Map

```
Email Intake ──[Published Language]──> Document Processing
                                              │
                                    [Domain Events]
                                              │
                                              v
Underwriting Guidelines <──[Conformist]── Submission Management
                                              │
                                    [Anti-Corruption Layer]
                                              │
                                              v
                                         Routing
```

### Application Services

| Service | Context | Responsibility |
|---------|---------|----------------|
| `EmailPollingWorker` | Email Intake | Polls shared mailboxes for new emails |
| `ProcessInboundEmailHandler` | Email Intake | Processes emails, extracts attachments |
| `DocumentProcessingOrchestrator` | Document Processing | Coordinates document analysis pipeline |
| `AcordFormExtractor` | Document Processing | Extracts data from ACORD forms |
| `SubmissionCreationService` | Submission | Creates submissions from processed data |
| `DataQualityScoringService` | Submission | Calculates data quality scores |
| `RoutingEngine` | Routing | Evaluates rules, assigns underwriters |

### Layered Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Presentation Layer                          │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐             │
│  │ Vector.Api   │ │ Producer     │ │ Underwriter  │             │
│  │ (REST API)   │ │ Portal       │ │ Dashboard    │             │
│  └──────────────┘ └──────────────┘ └──────────────┘             │
├─────────────────────────────────────────────────────────────────┤
│                      Application Layer                           │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Vector.Application (Commands, Queries, Handlers, DTOs)   │   │
│  │ MediatR Pipeline Behaviors (Validation, Logging, Tx)     │   │
│  └──────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────┤
│                        Domain Layer                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Vector.Domain (Aggregates, Entities, Value Objects,      │   │
│  │                Domain Events, Domain Services)            │   │
│  └──────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────┤
│                     Infrastructure Layer                         │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Vector.Infrastructure (Repositories, External Services,  │   │
│  │                        Message Bus, Caching, Storage)     │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Technology Architecture

### Technology Stack

| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| **Runtime** | .NET | 9.0 | Application framework |
| **Language** | C# | 13 | Primary language |
| **Web Framework** | ASP.NET Core | 9.0 | REST API, web portals |
| **ORM** | Entity Framework Core | 9.0 | Data access |
| **Messaging** | MediatR | 12.x | In-process messaging (CQRS) |
| **Database** | SQL Server | 2022 | Primary data store |
| **Cache** | Redis | 7.x | Distributed caching |
| **Blob Storage** | Azure Blob Storage | - | Document storage |
| **Message Bus** | Azure Service Bus | - | Async messaging |
| **Document AI** | Azure Document Intelligence | - | Document parsing |
| **Email** | Microsoft Graph API | 5.x | Email integration |
| **Frontend** | Blazor Server | 9.0 | Web portals |
| **Containers** | Docker | - | Containerization |
| **Orchestration** | Kubernetes (AKS) | - | Container orchestration |

### Infrastructure Components

```
┌─────────────────────────────────────────────────────────────────┐
│                     Azure Kubernetes Service                     │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │
│  │ Vector.Api  │ │ Vector.Web  │ │ Vector.     │               │
│  │ (Pods: 3)   │ │ (Pods: 2)   │ │ Worker (2)  │               │
│  └─────────────┘ └─────────────┘ └─────────────┘               │
├─────────────────────────────────────────────────────────────────┤
│                      Azure Services                              │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐           │
│  │ SQL      │ │ Redis    │ │ Service  │ │ Blob     │           │
│  │ Database │ │ Cache    │ │ Bus      │ │ Storage  │           │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘           │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                        │
│  │ Document │ │ Key      │ │ App      │                        │
│  │ Intel.   │ │ Vault    │ │ Insights │                        │
│  └──────────┘ └──────────┘ └──────────┘                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Architecture

### Domain Model

#### Submission Aggregate
```
Submission (Aggregate Root)
├── Id: Guid
├── TenantId: Guid
├── SubmissionNumber: string
├── Status: SubmissionStatus
├── Insured: InsuredParty (Value Object)
├── Coverages: Collection<Coverage> (Entity)
├── Locations: Collection<ExposureLocation> (Entity)
├── LossHistory: Collection<LossHistoryItem> (Entity)
├── AppetiteScore: int?
├── WinnabilityScore: int?
├── DataQualityScore: int?
└── QuotedPremium: Money? (Value Object)
```

#### Processing Job Aggregate
```
ProcessingJob (Aggregate Root)
├── Id: Guid
├── TenantId: Guid
├── Status: ProcessingStatus
├── Documents: Collection<ProcessedDocument> (Entity)
│   ├── DocumentType: DocumentType
│   ├── ExtractedFields: Collection<ExtractedField>
│   └── ConfidenceScore: decimal
└── DataQualityScore: int?
```

### Database Schema

| Table | Description | Indexes |
|-------|-------------|---------|
| `Submissions` | Core submission data | `IX_TenantId_Status`, `IX_SubmissionNumber` |
| `Coverages` | Coverage requests | `IX_SubmissionId` |
| `ExposureLocations` | Location/property data | `IX_SubmissionId` |
| `LossHistory` | Historical claims | `IX_SubmissionId` |
| `InboundEmails` | Received emails | `IX_TenantId_ReceivedAt` |
| `ProcessingJobs` | Document processing jobs | `IX_TenantId_Status` |
| `UnderwritingGuidelines` | Appetite rules | `IX_TenantId_IsActive` |
| `RoutingRules` | Assignment rules | `IX_Priority_IsActive` |

### Data Flow

```
Email → InboundEmail → ProcessingJob → Submission
                            ↓
                    ProcessedDocument
                            ↓
                    ExtractedField
```

---

## Integration Architecture

### External Systems

| System | Integration Pattern | Protocol | Purpose |
|--------|---------------------|----------|---------|
| Microsoft 365 | Webhook/Polling | Microsoft Graph API | Email ingestion |
| Policy Admin System | Event-Driven | REST API | Policy creation |
| CRM System | Bi-directional Sync | REST API | Customer data |
| Document Intelligence | Request-Response | REST API | Document parsing |

### API Contracts

#### REST API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/submissions` | GET | List submissions |
| `/api/v1/submissions` | POST | Create submission |
| `/api/v1/submissions/{id}` | GET | Get submission details |
| `/api/v1/submissions/by-number/{number}` | GET | Get by submission number |
| `/api/v1/submissions/{id}/assign` | POST | Assign underwriter |
| `/api/v1/submissions/{id}/quote` | POST | Quote submission |
| `/api/v1/submissions/{id}/decline` | POST | Decline submission |
| `/api/v1/submissions/{id}/bind` | POST | Bind submission |

### Message Contracts

| Queue | Message Type | Publisher | Consumer |
|-------|--------------|-----------|----------|
| `email-ingestion` | EmailReceivedEvent | EmailPollingWorker | ProcessingWorker |
| `document-processing` | DocumentProcessingCommand | ProcessingWorker | AIService |
| `submission-events` | SubmissionCreatedEvent | SubmissionService | RoutingEngine |

---

## Security Architecture

### Authentication & Authorization

```
┌─────────────────────────────────────────────────────────────────┐
│                    Microsoft Entra ID                            │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │
│  │ Producer    │ │ Underwriter │ │ Admin       │               │
│  │ Role        │ │ Role        │ │ Role        │               │
│  └─────────────┘ └─────────────┘ └─────────────┘               │
└─────────────────────────────────────────────────────────────────┘
                              │
                         JWT Tokens
                              │
                              v
┌─────────────────────────────────────────────────────────────────┐
│                      Vector API                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Authorization Middleware                                  │   │
│  │ - Role-Based Access Control (RBAC)                       │   │
│  │ - Resource-Based Policies                                │   │
│  │ - Tenant Isolation (Query Filters)                       │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Security Controls

| Control | Implementation | Purpose |
|---------|----------------|---------|
| Authentication | Microsoft Entra ID + JWT | Identity verification |
| Authorization | RBAC + Resource policies | Access control |
| Encryption at Rest | AES-256 | Data protection |
| Encryption in Transit | TLS 1.3 | Network security |
| Secrets Management | Azure Key Vault | Credential storage |
| Audit Logging | Temporal tables + Event log | Compliance |
| Multi-Tenancy | EF Core query filters | Data isolation |

---

## Deployment Architecture

### Environment Strategy

| Environment | Purpose | Infrastructure |
|-------------|---------|----------------|
| Development | Local development | Docker Compose |
| Test | Automated testing | AKS (Dev) |
| Staging | Pre-production validation | AKS (Staging) |
| Production | Live system | AKS (Production) |

### Kubernetes Deployment

```yaml
Namespace: vector
├── Deployments
│   ├── vector-api (replicas: 3)
│   ├── vector-web-producer (replicas: 2)
│   ├── vector-web-underwriting (replicas: 2)
│   └── vector-worker (replicas: 2)
├── Services
│   ├── vector-api (ClusterIP)
│   └── vector-web (ClusterIP)
├── Ingress
│   └── vector-ingress (NGINX)
├── ConfigMaps
│   └── vector-config
├── Secrets
│   └── vector-secrets
└── HorizontalPodAutoscalers
    ├── vector-api-hpa
    └── vector-worker-hpa
```

### Health Monitoring

| Endpoint | Purpose | Probe Type |
|----------|---------|------------|
| `/health` | Overall health | - |
| `/health/live` | Liveness | Kubernetes liveness |
| `/health/ready` | Readiness | Kubernetes readiness |

---

## Diagrams

The following ArchiMate diagrams are available in the `docs/diagrams/` directory:

1. **Business Layer Diagram** (`business-layer.puml`)
2. **Application Layer Diagram** (`application-layer.puml`)
3. **Technology Layer Diagram** (`technology-layer.puml`)
4. **Full Architecture Diagram** (`full-architecture.puml`)
5. **LeanIX Import File** (`vector-archimate.xml`)

To generate PNG images from PlantUML files:
```bash
java -jar plantuml.jar docs/diagrams/*.puml
```

Or use the PlantUML VS Code extension.

---

## References

- [ArchiMate 3.2 Specification](https://pubs.opengroup.org/architecture/archimate3-doc/)
- [Domain-Driven Design Reference](https://www.domainlanguage.com/ddd/reference/)
- [Microsoft Azure Well-Architected Framework](https://docs.microsoft.com/en-us/azure/architecture/framework/)

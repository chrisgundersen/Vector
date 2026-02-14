# Vector - Enterprise Insurance Submission Management System

Vector is an enterprise-grade system for ingesting, parsing, routing, and managing insurance submissions from wholesale and retail brokers in the P&C (Property & Casualty) space.

## Features

- **Email Ingestion**: Automated monitoring of shared mailboxes via Microsoft Graph API
- **Document Processing**: AI-powered parsing of ACORD forms, loss runs, and exposure schedules using Azure Document Intelligence
- **Intelligent Routing**: Configurable rules-based routing to underwriters
- **Underwriting Guidelines**: WYSIWYG builder for appetite rules and eligibility criteria
- **Multi-Tenant**: Full tenant isolation with configurable settings
- **Real-Time Updates**: SignalR-based dashboards for underwriters and producers

## Quick Start

### Prerequisites

- .NET 9 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code with C# extension

### 1. Start Infrastructure Services

```bash
# Start SQL Server, Redis, and Azurite (Azure Storage emulator)
docker-compose -f docker-compose.infrastructure.yml up -d
```

This starts:
- **SQL Server** on port `1433` (User: `sa`, Password: `Vector_P@ssw0rd!`)
- **Redis** on port `6379`
- **Azurite** (Blob: `10000`, Queue: `10001`, Table: `10002`)
- **Adminer** (DB UI) on port `8080`
- **Redis Commander** on port `8081`

### 2. Run the API

```bash
cd src/Vector.Api
dotnet run
```

The API will:
1. Apply database migrations automatically
2. Seed sample data (10 submissions, guidelines, routing rules)
3. Start listening on `http://localhost:5000`

### 3. Access the Application

| Service | URL |
|---------|-----|
| Swagger UI | http://localhost:5000 |
| API Endpoints | http://localhost:5000/api/v1/* |
| Health Check | http://localhost:5000/health |
| Adminer (DB) | http://localhost:8080 |
| Redis Commander | http://localhost:8081 |

## Testing the API

### Using VS Code REST Client

Open `tests/Vector.Api.http` in VS Code with the REST Client extension installed. You can execute requests directly from the file.

### Using curl

```bash
# Health check
curl http://localhost:5000/health

# Get all submissions
curl -H "X-Tenant-Id: 00000000-0000-0000-0000-000000000001" \
     http://localhost:5000/api/v1/submissions

# Create a submission
curl -X POST http://localhost:5000/api/v1/submissions \
     -H "Content-Type: application/json" \
     -H "X-Tenant-Id: 00000000-0000-0000-0000-000000000001" \
     -d '{"submissionNumber":"SUB-TEST-001","insuredName":"Test Company"}'
```

### End-to-End Workflow

1. **Create Submission** → `POST /api/v1/submissions`
2. **Mark Received** → (automatic on creation)
3. **Assign Underwriter** → `POST /api/v1/submissions/{id}/assign`
4. **Quote** → `POST /api/v1/submissions/{id}/quote`
5. **Bind** → `POST /api/v1/submissions/{id}/bind`

## Seeded Test Data

### Tenants
| ID | Description |
|----|-------------|
| `00000000-0000-0000-0000-000000000001` | Default tenant |
| `00000000-0000-0000-0000-000000000002` | Second tenant |

### Underwriters
| ID | Name |
|----|------|
| `11111111-1111-1111-1111-111111111111` | John Smith |
| `22222222-2222-2222-2222-222222222222` | Jane Doe |

### Sample Submissions (Default Tenant)

| Number | Insured | Status |
|--------|---------|--------|
| SUB-2024-000001 | Acme Manufacturing Inc | Draft |
| SUB-2024-000002 | Beta Retail Corp | Received |
| SUB-2024-000003 | Gamma Technologies LLC | InReview |
| SUB-2024-000004 | Delta Construction Co | PendingInformation |
| SUB-2024-000005 | Epsilon Logistics Inc | Quoted |
| SUB-2024-000006 | Zeta Healthcare Systems | Quoted |
| SUB-2024-000007 | Eta Financial Services | Bound |
| SUB-2024-000008 | Theta Mining Corp | Declined |

## Project Structure

```
Vector/
├── src/
│   ├── Vector.Domain/           # Domain layer (aggregates, entities, value objects)
│   ├── Vector.Application/      # Application layer (commands, queries, handlers)
│   ├── Vector.Infrastructure/   # Infrastructure (EF Core, Azure services, caching)
│   ├── Vector.Api/              # REST API (controllers, middleware)
│   ├── Vector.Worker/           # Background services
│   ├── Vector.Web.Producer/     # Producer Portal (Blazor)
│   ├── Vector.Web.Admin/        # MGU Admin Portal (Blazor)
│   └── Vector.Web.Underwriting/ # Underwriter Dashboard (Blazor)
├── tests/
│   ├── Vector.Domain.UnitTests/
│   ├── Vector.Application.UnitTests/
│   ├── Vector.Infrastructure.IntegrationTests/
│   └── Vector.Api.IntegrationTests/
└── docker-compose.infrastructure.yml
```

## Configuration

### Development Settings (`appsettings.Development.json`)

```json
{
  "UseInMemoryDatabase": false,
  "UseMockServices": true,
  "SeedDatabase": true,
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=VectorDb;..."
  }
}
```

| Setting | Description |
|---------|-------------|
| `UseInMemoryDatabase` | Use EF Core in-memory DB (no SQL Server needed) |
| `UseMockServices` | Use mock implementations for external services |
| `SeedDatabase` | Seed sample data on startup |

### Running with In-Memory Database (No Docker)

```bash
# Edit appsettings.Development.json
# Set "UseInMemoryDatabase": true

dotnet run --project src/Vector.Api
```

## Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test --filter "FullyQualifiedName~UnitTests"

# Integration tests only
dotnet test --filter "FullyQualifiedName~IntegrationTests"

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture

### Bounded Contexts

| Context | Responsibility |
|---------|----------------|
| Email Intake | Monitor mailboxes, extract emails/attachments |
| Document Processing | OCR, parse ACORD forms, loss runs |
| Submission | Core submission lifecycle, coverages, exposures |
| Underwriting Guidelines | MGU appetite rules, eligibility criteria |
| Routing | Match submissions to underwriters |

### Technology Stack

- **.NET 9 / C# 13** - Latest language features
- **ASP.NET Core** - REST API with MediatR (CQRS)
- **Entity Framework Core 9** - SQL Server with temporal tables
- **Azure Services** - Document Intelligence, Service Bus, Blob Storage
- **Redis** - Distributed caching
- **Blazor Server** - Real-time web portals

## API Documentation

Full OpenAPI documentation is available at `/swagger` when running in development mode.

### Key Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/submissions` | List submissions |
| POST | `/api/v1/submissions` | Create submission |
| GET | `/api/v1/submissions/{id}` | Get submission details |
| POST | `/api/v1/submissions/{id}/assign` | Assign underwriter |
| POST | `/api/v1/submissions/{id}/quote` | Quote submission |
| POST | `/api/v1/submissions/{id}/decline` | Decline submission |
| POST | `/api/v1/submissions/{id}/request-info` | Request information |
| POST | `/api/v1/submissions/{id}/bind` | Bind submission |

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| `X-Tenant-Id` | Yes | Tenant identifier (GUID) |
| `X-Correlation-Id` | No | Request correlation ID (auto-generated if not provided) |

## Troubleshooting

### SQL Server Connection Failed

1. Ensure Docker is running: `docker ps`
2. Check container health: `docker-compose -f docker-compose.infrastructure.yml ps`
3. Wait for SQL Server to be ready (can take 30+ seconds on first start)

### Database Migration Errors

```bash
# Reset the database
docker-compose -f docker-compose.infrastructure.yml down -v
docker-compose -f docker-compose.infrastructure.yml up -d
```

### Port Conflicts

Edit `docker-compose.infrastructure.yml` to change port mappings if needed.

## Contributing

1. Follow the coding standards in `CLAUDE.md`
2. Write tests first (TDD)
3. Ensure all tests pass: `dotnet test`
4. Create a PR with descriptive title and summary

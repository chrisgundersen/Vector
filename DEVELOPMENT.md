# Vector Development Guide

This document explains how to set up, run, and debug the Vector application locally.

## Prerequisites

- .NET 9.0 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code with C# extension
- Node.js (for any frontend tooling)

## Solution Structure

```
Vector/
├── src/
│   ├── Vector.Api/                    # REST API (runs on port 5148)
│   ├── Vector.Web.Underwriting/       # Unified Blazor web app (runs on port 5176)
│   ├── Vector.Application/            # Application layer (MediatR handlers)
│   ├── Vector.Domain/                 # Domain layer (entities, aggregates)
│   └── Vector.Infrastructure/         # Infrastructure (EF Core, repositories)
├── tools/
│   └── Vector.DataLoader/             # Test data seeding tool
└── tests/
    ├── Vector.Domain.UnitTests/
    ├── Vector.Application.UnitTests/
    ├── Vector.Infrastructure.IntegrationTests/
    ├── Vector.Api.IntegrationTests/
    └── Vector.Web.UITests/
```

## Quick Start

### 1. Restore Dependencies

```bash
dotnet restore
```

### 2. Build the Solution

```bash
dotnet build
```

### 3. Run Tests

```bash
dotnet test
```

### 4. Start the Application

You need to run both the API and Web projects for full functionality.

**Option A: Using multiple terminals**

Terminal 1 - API:
```bash
dotnet run --project src/Vector.Api
```

Terminal 2 - Web UI:
```bash
dotnet run --project src/Vector.Web.Underwriting
```

**Option B: Using Visual Studio**

Set multiple startup projects:
1. Right-click the solution → "Set Startup Projects"
2. Select "Multiple startup projects"
3. Set both `Vector.Api` and `Vector.Web.Underwriting` to "Start"

### 5. Access the Application

- **Web UI**: http://localhost:5176
- **API**: http://localhost:5148
- **Swagger**: http://localhost:5148/swagger

## Loading Test Data

### Using the DataLoader Tool

The DataLoader seeds the database with realistic test data including:
- Test users (underwriters, producers, admins)
- Submissions with coverages, locations, and loss history
- Underwriting guidelines
- Producer-underwriter pairings
- Routing rules

```bash
# Run the DataLoader
dotnet run --project tools/Vector.DataLoader

# Or with verbose output
dotnet run --project tools/Vector.DataLoader -- --verbose
```

The DataLoader will:
1. Apply any pending database migrations
2. Seed test data if not already present
3. Output progress to the console

### Using the Simulation API

You can also create test submissions via the API:

```bash
# Create test submissions (requires API to be running)
curl -X POST http://localhost:5148/api/simulation/submissions \
  -H "Content-Type: application/json" \
  -d '{"count": 10}'
```

## Mock Authentication (Development Mode)

When running locally with `DisableAuthentication: true` in appsettings, you can switch between test users without real authentication.

### Available Test Users

**Underwriters:**
| Name | ID | Role |
|------|-----|------|
| John Smith | 11111111-1111-1111-1111-111111111111 | Underwriter |
| Jane Doe | 22222222-2222-2222-2222-222222222222 | Underwriter |
| Robert Johnson | 11111111-1111-1111-1111-111111111112 | Underwriter |

**Producers:**
| Name | ID | Role |
|------|-----|------|
| ABC Insurance Agency | 33333333-3333-3333-3333-333333333333 | Producer |
| Pacific Coast Brokers | 44444444-4444-4444-4444-444444444444 | Producer |
| Mountain State Insurance | 55555555-5555-5555-5555-555555555555 | Producer |
| Great Lakes Agency | 66666666-6666-6666-6666-666666666666 | Producer |

**Admins:**
| Name | ID | Role |
|------|-----|------|
| System Admin | 99999999-9999-9999-9999-999999999999 | Admin |

### Switching Users

1. Navigate to http://localhost:5176/login
2. Click on any user card to log in as that user
3. The navigation menu will update based on the user's role

## Role-Based Navigation

The web application shows different features based on user role:

### Underwriters
- **Dashboard** (`/`) - Overview of submission metrics
- **Submission Queue** (`/queue`) - New submissions awaiting review
- **All Submissions** (`/submissions`) - Browse all submissions
- **My Work** (`/my-work`) - Submissions assigned to you

### Producers
- **Dashboard** (`/producer`) - Your submission overview
- **My Submissions** (`/producer/submissions`) - List of your submissions
- **Data Corrections** (`/producer/corrections`) - Correction requests

### Admins
- **Guidelines** (`/admin/guidelines`) - Manage underwriting guidelines
- **Routing Rules** (`/admin/routing`) - Configure submission routing
- **Pairings** (`/admin/pairings`) - Producer-underwriter assignments

## Configuration

### appsettings.Local.json

```json
{
  "ConnectionStrings": {
    "VectorDb": "Server=(localdb)\\mssqllocaldb;Database=Vector;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "AuthSettings": {
    "DisableAuthentication": true
  }
}
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Development |
| `ConnectionStrings__VectorDb` | Database connection string | LocalDB |

## Debugging in Visual Studio

### Debugging Multiple Projects

1. Set solution to start multiple projects (Api + Web.Underwriting)
2. Set breakpoints in any project
3. Press F5 to start debugging

### Debugging the DataLoader

1. Right-click `Vector.DataLoader` → "Set as Startup Project"
2. Press F5 to debug

### Useful Breakpoint Locations

- `SimulationController.CreateTestSubmissions` - Test data creation
- `DatabaseSeeder.SeedAsync` - Initial data seeding
- `ProcessInboundEmailCommandHandler.Handle` - Email processing
- `MockUserService.SetCurrentUser` - User switching

## Database Management

### Apply Migrations

```bash
dotnet ef database update --project src/Vector.Infrastructure --startup-project src/Vector.Api
```

### Add a New Migration

```bash
dotnet ef migrations add MigrationName --project src/Vector.Infrastructure --startup-project src/Vector.Api
```

### Reset Database

```bash
dotnet ef database drop --project src/Vector.Infrastructure --startup-project src/Vector.Api
dotnet ef database update --project src/Vector.Infrastructure --startup-project src/Vector.Api
```

## Troubleshooting

### "Submissions not appearing in UI"

1. Ensure the API is running (check http://localhost:5148/swagger)
2. Run the DataLoader to seed test data
3. Check the browser console for errors
4. Verify the database has data: check the `Submissions` table

### "Login page not showing users"

1. Ensure `DisableAuthentication: true` in appsettings
2. Restart the Web.Underwriting application
3. Navigate to http://localhost:5176/login

### "Producer submissions empty"

1. Verify you're logged in as a Producer (check the role in the header)
2. Ensure submissions are associated with that producer ID
3. Run DataLoader to create properly associated test data

### "Database connection errors"

1. Ensure SQL Server LocalDB is installed and running
2. Check the connection string in appsettings
3. Try: `sqllocaldb start MSSQLLocalDB`

## API Endpoints

### Submissions
- `GET /api/v1/submissions` - List submissions (paginated)
- `GET /api/v1/submissions/{id}` - Get submission details
- `POST /api/v1/submissions/{id}/assign` - Assign to underwriter
- `POST /api/v1/submissions/{id}/quote` - Quote a submission
- `POST /api/v1/submissions/{id}/decline` - Decline a submission

### Simulation (Development Only)
- `POST /api/simulation/submissions` - Create test submissions
- `POST /api/simulation/email` - Simulate email ingestion

### Health
- `GET /health` - Health check endpoint

## Running All Services for Full Testing

To run a complete development environment:

```bash
# Terminal 1: Start the API
dotnet run --project src/Vector.Api

# Terminal 2: Start the Web UI
dotnet run --project src/Vector.Web.Underwriting

# Terminal 3: Load test data (run once)
dotnet run --project tools/Vector.DataLoader
```

Then:
1. Open http://localhost:5176/login
2. Select a test user to log in
3. Explore the application based on your role

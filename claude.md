# Claude Code Instructions

> Comprehensive .NET/C# development guidelines emphasizing best practices, security, performance, and maintainability.

**Version:** 1.0 | **Last Updated:** 2025-11-19

---

## Table of Contents

1. [Core Development Workflow](#core-development-workflow)
2. [C# & .NET Standards](#c--net-standards)
3. [Architecture & Design Patterns](#architecture--design-patterns)
4. [Testing Requirements](#testing-requirements)
5. [Security Guidelines](#security-guidelines)
6. [Performance & Optimization](#performance--optimization)
7. [Containerization](#containerization)

---

## Core Development Workflow

### Language Policy
- **All content in English**: Instructions, code comments, documentation, commits, PRs

### Implementation Workflow

**ALWAYS follow this sequence:**

1. **List Instructions Used**: State which sections guide implementation (e.g., "Using: [C# Standards, Security Guidelines]")
2. **Write Tests First (TDD)**: Create/modify test cases before implementation
3. **Run Tests**: Execute `dotnet test` or `dotnet watch test` - don't ask, just run
4. **Fix All Issues**: Address warnings and errors before proceeding
5. **Path Conventions**: Replace `[project]` and `[feature]` placeholders with actual names

### Pre-Implementation Analysis (MANDATORY)

**Before coding, explicitly state:**
- DDD patterns and SOLID principles applicable
- Affected layers (Domain/Application/Infrastructure)
- Alignment with ubiquitous language
- Security and compliance considerations
- Aggregate boundaries and domain rules
- Test naming: `MethodName_Condition_ExpectedResult()`

**If unclear on any point, STOP and ask for clarification.**

---

## C# & .NET Standards

### Language & Formatting

- **Version**: C# 13 (use latest features: records, pattern matching, file-scoped namespaces, global usings)
- **Naming**: PascalCase (public), camelCase (private), "I" prefix for interfaces
- **Nullability**: Use `is null`/`is not null`, trust null annotations, declare non-nullable by default
- **Formatting**: Follow `.editorconfig`, use `nameof`, pattern matching, switch expressions
- **Comments**: Focus on "why" not "what", use `<summary>`, `<param>`, `<returns>`, `<exception>`, `<example>`

### Core Patterns

- **Primary Constructors**: Use for DI (e.g., `public class MyClass(IDependency dep)`)
- **Async/Await**: All I/O operations, return `Task`/`Task<T>`, use `ConfigureAwait(false)` where appropriate
- **Dependency Injection**: Constructor injection with `ArgumentNullException` null checks
- **Resource Management**: Use `ResourceManager` for localized messages, separate .resx files

### Project Structure

- **Namespace**: `{Core|Console|App|Service}.{Feature}`
- **Layers**: Domain → Application → Infrastructure → API/UI
- **Separation**: Models, services, data access in distinct layers

---

## Architecture & Design Patterns

### Domain-Driven Design (DDD)

**Core Principles:**
- **Ubiquitous Language**: Consistent business terms across code/docs
- **Bounded Contexts**: Clear service boundaries
- **Aggregates**: Consistency boundaries, transactional integrity
- **Domain Events**: Capture business-significant state changes
- **Rich Domain Models**: Business logic in domain layer, not application services

**Layer Responsibilities:**
- **Domain**: Aggregates, value objects, domain services, domain events, specifications
- **Application**: Application services (orchestration), DTOs, input validation
- **Infrastructure**: Repositories, event bus, data mappers/ORMs, external adapters

### SOLID Principles

- **SRP**: One reason to change per class
- **OCP**: Open for extension, closed for modification
- **LSP**: Subtypes substitutable for base types
- **ISP**: No forced dependencies on unused methods
- **DIP**: Depend on abstractions, not concretions

### Required Design Patterns

- **Command**: Generic base classes, `ICommandHandler<TOptions>`, static setup methods
- **Factory**: Complex object creation, service provider integration
- **Repository**: Async data access, provider abstractions for connections
- **Provider**: External service abstractions, clear contracts, config handling

### Financial Domain Requirements

- **Monetary Values**: Use `decimal`, currency-aware value objects, proper rounding
- **Transactions**: Saga patterns for distributed transactions, domain events for eventual consistency
- **Audit**: Capture all operations as domain events, immutable audit trails
- **Calculations**: Encapsulate in domain services, maintain history

### Quality Validation (MANDATORY before delivery)

**Domain Design:**
- ✓ Aggregates model business concepts correctly
- ✓ Ubiquitous language consistent
- ✓ SOLID principles followed
- ✓ Domain logic encapsulated in aggregates
- ✓ Domain events properly published/handled

**Implementation:**
- ✓ Tests follow `MethodName_Condition_ExpectedResult()` naming
- ✓ Performance considered
- ✓ Authorization at aggregate boundaries
- ✓ Domain decisions documented
- ✓ .NET best practices (async, DI, error handling)

**Financial (if applicable):**
- ✓ `decimal` types with proper rounding
- ✓ Transaction boundaries correct
- ✓ Audit capabilities complete
- ✓ PCI-DSS/SOX/LGPD requirements met

---

## Testing Requirements

### Framework & Libraries

- **Framework**: xUnit v3
- **Mocks**: Moq
- **Contract Tests**: Testcontainers, Microcks
- **Assertions**: FluentAssertions

### Test Naming Convention

**Pattern**: `MethodName_Condition_ExpectedResult()`

Examples:
- `AddUser_WithValidData_ReturnsCreatedUser()`
- `ProcessPayment_WithInsufficientFunds_ThrowsPaymentException()`

### Test Organization

**Unit Tests:**
- **Location**: `tests/[project].UnitTests/`
- **Scope**: Domain and Application layers only
- **Dependencies**: Mock with Moq, no real databases/services
- **Focus**: Business logic, use cases, validation

**Integration Tests:**
- **Location**: `tests/[project].IntegrationTests/`
- **Scope**: Infrastructure, API layers, cross-layer integration
- **Dependencies**: Testcontainers for real/simulated dependencies
- **Focus**: Complete business flows, persistence, external calls

### Test Structure (xUnit)

- No test class attributes required
- Use `[Fact]` for simple tests, `[Theory]` with `[InlineData]`/`[MemberData]`/`[ClassData]` for data-driven
- Follow **Arrange-Act-Assert (AAA)** pattern (no AAA comments)
- Constructor for setup, `IDisposable.Dispose()` for teardown
- `IClassFixture<T>` for shared context within class
- `ICollectionFixture<T>` for shared context across classes
- Use `[Trait("Category", "Name")]` for categorization

### Best Practices

- **TDD**: Write tests before implementation
- **Independence**: Tests run in any order, no interdependencies
- **Single Behavior**: One test, one behavior
- **Explicit Names**: Describe purpose clearly
- **Style**: Copy existing style from nearby files
- **Continuous Testing**: Run after every significant change

### Key Assertions

- `Assert.Equal`: Value equality
- `Assert.Same`: Reference equality
- `Assert.True`/`False`: Boolean conditions
- `Assert.Contains`/`DoesNotContain`: Collections
- `Assert.Throws<T>`: Sync exceptions
- `await Assert.ThrowsAsync<T>`: Async exceptions

---

## Security Guidelines

### Security-First Mindset

**Default to secure options. When in doubt, choose security and explain why.**

### OWASP Top 10 Protection

**A01: Broken Access Control & A10: SSRF**
- Principle of least privilege, deny by default
- Validate URLs with strict allow-lists
- Sanitize file paths to prevent traversal (`../../etc/passwd`)

**A02: Cryptographic Failures**
- Use Argon2/bcrypt for passwords (never MD5/SHA-1)
- Always HTTPS for network requests
- AES-256 for data at rest
- Never hardcode secrets - use environment variables or secrets managers

```csharp
// GOOD
var apiKey = Environment.GetEnvironmentVariable("API_KEY");
// BAD: var apiKey = "sk_hardcoded_secret";
```

**A03: Injection**
- **SQL**: Parameterized queries only, never string concatenation
- **Command**: Use built-in escaping functions
- **XSS**: Use `.textContent` over `.innerHTML`, use DOMPurify when HTML needed

**A05: Security Misconfiguration**
- Disable debug/verbose errors in production
- Set security headers: `Content-Security-Policy`, `Strict-Transport-Security`, `X-Content-Type-Options`
- Keep dependencies updated, scan with `npm audit`/`pip-audit`/Snyk

**A07: Authentication Failures**
- Generate new session ID on login (prevent fixation)
- Cookie attributes: `HttpOnly`, `Secure`, `SameSite=Strict`
- Rate limiting and account lockout

**A08: Integrity Failures**
- Avoid deserializing untrusted data
- Prefer JSON over binary formats
- Implement strict type checking

### General Security Rules

- Validate all inputs at boundaries
- Never expose sensitive info in errors
- Log security events (never log secrets)
- Explain what you're protecting against in comments

---

## Performance & Optimization

### General Principles

- **Measure First**: Profile before optimizing (Chrome DevTools, dotTrace, PerfView)
- **Optimize Common Case**: Focus on frequently executed paths
- **Avoid Premature Optimization**: Clear code first, optimize when necessary
- **Document Critical Code**: Comment on performance-critical sections
- **Automate Testing**: Performance tests in CI/CD

### Frontend

**Rendering & DOM:**
- Batch DOM updates, use `React.memo`/`useMemo`/`useCallback`
- Stable keys in lists, CSS animations over JS
- Defer non-critical rendering (`requestIdleCallback`)

**Assets:**
- Compress images (WebP/AVIF), lazy load (`loading="lazy"`)
- Minify/bundle JS/CSS with tree-shaking
- Long-lived cache headers with cache busting
- Subset fonts, use `font-display: swap`

**Network:**
- Reduce HTTP requests, enable HTTP/2/3
- Use CDNs, Service Workers, `defer`/`async` scripts
- Preload/prefetch critical resources

**JavaScript:**
- Offload heavy work to Web Workers
- Debounce/throttle scroll/resize/input events
- Clean up listeners/intervals to prevent leaks
- Use efficient data structures (Maps/Sets, TypedArrays)

### Backend

**Algorithms & Data:**
- Choose right data structures
- Avoid O(n²) or worse, profile loops/recursion
- Batch processing, streaming for large data

**Concurrency:**
- Async/await for I/O operations
- Thread/worker pools for concurrency management
- Proper synchronization (locks, semaphores)
- Batch network/database calls

**Caching:**
- Cache expensive computations (Redis/Memcached)
- Proper invalidation (TTL, event-based)
- Prevent cache stampede (locks, request coalescing)

**API & Network:**
- Minimize/compress payloads (gzip, Brotli)
- Paginate large result sets
- Rate limiting, connection pooling
- Use HTTP/2, gRPC, WebSockets

**.NET Specific:**
- `async`/`await` for I/O-bound operations
- `Span<T>`/`Memory<T>` for efficient memory access
- Object/connection pooling
- `IAsyncEnumerable<T>` for streaming

### Database

**Query Optimization:**
- Index frequently queried/filtered/joined columns
- Avoid `SELECT *`, use only needed columns
- Parameterized queries, analyze plans (`EXPLAIN`)
- Avoid N+1 queries with joins/batch queries
- Use `LIMIT`/`OFFSET` for large tables

**Schema:**
- Normalize (reduce redundancy) or denormalize (read-heavy workloads)
- Efficient data types, partition large tables
- Archive/purge old data regularly

**Transactions:**
- Keep short to reduce lock contention
- Lowest isolation level meeting needs
- Avoid long-running transactions

**EF Core Specific:**
- `AsNoTracking()` for read-only queries
- Pagination with `Skip()`/`Take()`
- `Include()` for eager loading
- Projection (`Select`) for required fields only
- Compiled queries for frequent operations

### Performance Review Checklist

- [ ] Algorithmic inefficiencies (O(n²)+)?
- [ ] Appropriate data structures?
- [ ] Caching with correct invalidation?
- [ ] Queries optimized, indexed, no N+1?
- [ ] Payloads paginated/streamed?
- [ ] Memory leaks or unbounded resources?
- [ ] Blocking operations in hot paths?
- [ ] Logging minimized in hot paths?
- [ ] Performance tests automated?

---

## Containerization

### Core Principles

- **Immutability**: Images never change; changes = new image
- **Portability**: Run consistently across environments
- **Isolation**: Process, resource, network, filesystem isolation
- **Efficiency**: Smaller images = faster builds, deploys, fewer vulnerabilities

### Dockerfile Best Practices

**1. Multi-Stage Builds (REQUIRED)**

```dockerfile
# Stage 1: Dependencies
FROM node:18-alpine AS deps
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production && npm cache clean --force

# Stage 2: Build
FROM node:18-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 3: Production
FROM node:18-alpine AS production
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY --from=build /app/dist ./dist
USER node
EXPOSE 3000
CMD ["node", "dist/main.js"]
```

**2. Base Images**
- Official, minimal images (`alpine`, `slim`, `distroless`)
- Specific version tags (not `latest`)
- Regular security updates

**3. Layer Optimization**
- Order: least → most frequently changing
- Combine related `RUN` commands with `&&`
- Clean up in same `RUN` layer
- Copy deps before source code (caching)

**4. .dockerignore**
```dockerignore
.git
node_modules
dist
build
.env
*.log
.vscode
README.md
test/
```

**5. Security**
- **Non-root user**: Always create and switch to non-root user
- **Minimal images**: Fewer packages = fewer vulnerabilities
- **No secrets**: Never include in layers, use runtime secrets
- **Scan**: Integrate Trivy/Snyk in CI/CD

```dockerfile
# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
RUN chown -R appuser:appgroup /app
USER appuser
```

**6. Health Checks**
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1
```

**7. Configuration**
```dockerfile
ENV NODE_ENV=production
ENV PORT=3000
ARG BUILD_VERSION
ENV APP_VERSION=$BUILD_VERSION
```

### Runtime Best Practices

**Resource Limits:**
```yaml
deploy:
  resources:
    limits:
      cpus: '0.5'
      memory: 512M
    reservations:
      cpus: '0.25'
      memory: 256M
```

**Logging:** STDOUT/STDERR, structured JSON, integrate with aggregators

**Storage:** Named volumes for persistence, never in writable layer

**Networking:** Custom networks for isolation, network policies

### Dockerfile Checklist

- [ ] Multi-stage build used?
- [ ] Minimal, versioned base image?
- [ ] Layers optimized (order, cleanup)?
- [ ] `.dockerignore` comprehensive?
- [ ] Non-root `USER` defined?
- [ ] `HEALTHCHECK` defined?
- [ ] No secrets in layers?
- [ ] Security scanning in CI/CD?

---

## Entity Framework Core

### Context Design
- Focused, cohesive `DbContext`
- Constructor injection for options
- `OnModelCreating` for fluent API
- `IEntityTypeConfiguration<T>` for entity configs
- `DbContextFactory` for console apps/tests

### Entity Design
- Meaningful primary keys
- Proper relationships (one-to-one, one-to-many, many-to-many)
- Data annotations or fluent API for constraints
- Appropriate navigational properties
- Owned entity types for value objects

### Performance
- `AsNoTracking()` for read-only
- `Include()` for eager loading
- Projection (`Select`) for required fields only
- Compiled queries for frequent operations
- Pagination with `Skip()`/`Take()`

### Migrations
- Small, focused, descriptively named
- Verify SQL before production
- Consider bundles for deployment
- Data seeding when appropriate

### Security
- Parameterized queries (avoid raw SQL)
- Data access permissions
- Encryption for sensitive data

---

## ASP.NET REST APIs

### API Design
- RESTful URLs, appropriate HTTP verbs
- Choose Controllers vs Minimal APIs based on complexity
- Proper status codes, content negotiation

### Controllers
- Attribute routing
- Model binding, validation, `[ApiController]`
- DI in constructors
- Return types: `IActionResult`, `ActionResult<T>`, or specific types

### Minimal APIs
- Endpoint routing, route groups
- Parameter binding, validation, DI
- Structure for maintainability

### Authentication & Authorization
- JWT Bearer tokens
- OAuth 2.0, OpenID Connect
- Role-based and policy-based authorization
- Microsoft Entra ID integration

### Validation & Error Handling
- Data annotations, FluentValidation
- Global exception handling middleware
- Consistent error responses
- Problem details (RFC 7807)

### Versioning & Documentation
- API versioning strategies
- Swagger/OpenAPI with comprehensive docs
- Document endpoints, params, responses, auth

### Logging & Monitoring
- Structured logging (Serilog)
- Application Insights integration
- Correlation IDs for request tracking

### Deployment
- Containerize: `dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer`
- CI/CD pipelines
- Health checks, readiness probes
- Environment-specific configs

---

## Blazor Development

### Structure
- Idiomatic Blazor/C# code
- Component-based UI, Razor Components
- Inline functions for simple, code-behind for complex
- Async/await for non-blocking UI

### Lifecycle
- `OnInitializedAsync`, `OnParametersSetAsync`
- Data binding with `@bind`
- DI for services

### Performance
- Choose Server/WASM based on requirements
- Async methods for API calls
- Optimize renders: `StateHasChanged()`, `ShouldRender()`
- `EventCallback` with minimal data

### State Management
- Cascading Parameters, EventCallbacks (basic)
- Fluxor, BlazorState (complex apps)
- Blazored.LocalStorage/SessionStorage (client-side)
- Scoped Services, StateContainer (server-side)

### Caching
- `IMemoryCache` (Server)
- localStorage/sessionStorage (WASM)
- Distributed cache (large apps)

### Security
- ASP.NET Identity or JWT
- HTTPS, proper CORS

---

## Code Quality Checklist

### Design Patterns
- [ ] Command, Factory, Repository, Provider patterns correct?
- [ ] SOLID principles followed?

### Architecture
- [ ] Namespace conventions: `{Core|Console|App|Service}.{Feature}`?
- [ ] Proper layer separation?

### .NET Best Practices
- [ ] Primary constructors, async/await, structured logging?
- [ ] ResourceManager, strongly-typed config?

### Testing
- [ ] `MethodName_Condition_ExpectedResult()` naming?
- [ ] AAA pattern, no interdependencies?

### Security
- [ ] Input validation, parameterized queries?
- [ ] Secure credential handling, no hardcoded secrets?

### Performance
- [ ] Async/await, resource disposal, caching?
- [ ] Queries optimized, no N+1?

### Documentation
- [ ] XML docs for public APIs?
- [ ] Meaningful names, clear intent?

---

## Final Reminders

**These are not guidelines—they are standards. Follow rigorously.**

**Before ANY implementation:**
1. State which sections guide you
2. Write tests first (TDD)
3. Run tests, fix all issues
4. Validate against quality checklists

**When in doubt:** Ask for clarification rather than assume.

**Token Optimization:** This document is optimized for minimal token usage. All critical requirements are preserved in concise, actionable format.

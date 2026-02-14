# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Vector.sln .
COPY src/Vector.Domain/Vector.Domain.csproj src/Vector.Domain/
COPY src/Vector.Application/Vector.Application.csproj src/Vector.Application/
COPY src/Vector.Infrastructure/Vector.Infrastructure.csproj src/Vector.Infrastructure/
COPY src/Vector.Web.Underwriting/Vector.Web.Underwriting.csproj src/Vector.Web.Underwriting/

# Restore dependencies
RUN dotnet restore src/Vector.Web.Underwriting/Vector.Web.Underwriting.csproj

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/Vector.Web.Underwriting/Vector.Web.Underwriting.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system appgroup && adduser --system --group appuser

# Copy published app
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Vector.Web.Underwriting.dll"]

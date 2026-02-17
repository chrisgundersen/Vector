using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="VectorDbContext"/> at design time (EF Core CLI migrations).
/// Uses SQL Server as the migration target provider.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VectorDbContext>
{
    public VectorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VectorDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=VectorDesignTime;Trusted_Connection=True");

        return new VectorDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }

    /// <summary>
    /// Minimal stub satisfying <see cref="ICurrentUserService"/> for design-time migration generation.
    /// </summary>
    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => null;
        public Guid? TenantId => null;
        public IReadOnlyCollection<string> Roles => [];
        public bool IsAuthenticated => false;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Licensing.Infrastructure.Persistence;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LicensingDbContext>
{
    public LicensingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LicensingDbContext>()
            .UseNpgsql("Host=localhost;Database=licensing;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new LicensingDbContext(options);
    }
}
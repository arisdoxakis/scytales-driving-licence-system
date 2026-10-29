using Licensing.Domain.Applications;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Licensing.Infrastructure.Persistence;

public sealed class LicensingDbContext(DbContextOptions<LicensingDbContext> options) : DbContext(options)
{
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LicensingDbContext).Assembly);

        // MassTransit outbox tables
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
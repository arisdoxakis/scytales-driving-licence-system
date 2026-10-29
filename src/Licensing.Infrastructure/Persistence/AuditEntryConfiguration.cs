using Licensing.Domain.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licensing.Infrastructure.Persistence;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("audit_entries");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();

        b.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(30);
        b.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(30);
        b.Property(e => e.Event).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.Details).HasMaxLength(2000);
        b.Property(e => e.Actor).HasMaxLength(100);

        b.HasIndex(e => new { e.ApplicationId, e.Timestamp });
    }
}
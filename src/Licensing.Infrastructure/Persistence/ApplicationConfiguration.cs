using Licensing.Domain.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licensing.Infrastructure.Persistence;

internal sealed class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> b)
    {
        b.ToTable("applications");
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).ValueGeneratedNever();

        b.ComplexProperty(a => a.PersonalDetails, pd =>
        {
            pd.Property(p => p.FirstName).HasColumnName("first_name").HasMaxLength(100);
            pd.Property(p => p.LastName).HasColumnName("last_name").HasMaxLength(100);
            pd.Property(p => p.DateOfBirth).HasColumnName("date_of_birth");
            pd.Property(p => p.NationalId).HasColumnName("national_id").HasMaxLength(50);
            pd.Property(p => p.Email).HasColumnName("email").HasMaxLength(254);
            pd.Property(p => p.PhoneNumber).HasColumnName("phone_number").HasMaxLength(30);
            pd.Property(p => p.Address).HasColumnName("address").HasMaxLength(300);
        });

        b.ComplexProperty(a => a.Photo, p =>
        {
            p.Property(x => x.StoragePath).HasMaxLength(300);
            p.Property(x => x.ContentType).HasMaxLength(100);
        });

        b.Property(a => a.LicenceCategory).HasConversion<string>().HasMaxLength(10);
        b.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(a => a.RejectionReason).HasMaxLength(1000);

        b.Property(a => a.Version).IsRowVersion();

        b.HasMany(a => a.History)
            .WithOne()
            .HasForeignKey(e => e.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(a => a.History).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.Ignore(a => a.IsTerminal);
    }
}
using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Librory.Infrastructure.Persistence.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("members");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PreferredLanguage).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.FamilyId, x.DisplayName }).IsUnique();

        builder.OwnsMany(x => x.ExternalIdentities, owned =>
        {
            owned.ToTable("member_external_identities");
            owned.WithOwner().HasForeignKey("MemberId");
            owned.Property<Guid>("MemberId");
            owned.Property(identity => identity.Provider).HasConversion<string>().HasMaxLength(32);
            owned.Property(identity => identity.ProviderSubject).HasMaxLength(200);
            owned.Property(identity => identity.Email).HasMaxLength(256);
            owned.Property(identity => identity.DisplayName).HasMaxLength(200);
            owned.Property(identity => identity.LinkedAt);
            owned.HasKey("MemberId", nameof(ExternalIdentity.Provider), nameof(ExternalIdentity.ProviderSubject));
            owned.HasIndex(identity => new { identity.Provider, identity.ProviderSubject }).IsUnique();
        });

        builder.HasOne(x => x.Family)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

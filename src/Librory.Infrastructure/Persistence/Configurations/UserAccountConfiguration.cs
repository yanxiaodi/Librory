using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Librory.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.HasIndex(x => x.Email).IsUnique();

        builder.OwnsMany(x => x.ExternalIdentities, owned =>
        {
            owned.ToTable("user_account_external_identities");
            owned.WithOwner().HasForeignKey("UserAccountId");
            owned.Property<Guid>("UserAccountId");
            owned.Property(identity => identity.Provider).HasConversion<string>().HasMaxLength(32);
            owned.Property(identity => identity.ProviderSubject).HasMaxLength(200);
            owned.Property(identity => identity.Email).HasMaxLength(256);
            owned.Property(identity => identity.DisplayName).HasMaxLength(200);
            owned.Property(identity => identity.LinkedAt);
            owned.HasKey("UserAccountId", nameof(ExternalIdentity.Provider), nameof(ExternalIdentity.ProviderSubject));
            owned.HasIndex(identity => new { identity.Provider, identity.ProviderSubject }).IsUnique();
        });
    }
}

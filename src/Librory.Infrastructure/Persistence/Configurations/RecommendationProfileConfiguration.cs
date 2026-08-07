using System.Text.Json;
using Librory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Librory.Infrastructure.Persistence.Configurations;

internal sealed class RecommendationProfileConfiguration : IEntityTypeConfiguration<RecommendationProfile>
{
    private static readonly ValueConverter<List<string>, string> StringListConverter = new(
        value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
        value => JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null) ?? new List<string>());

    private static readonly ValueConverter<List<PreferredLanguage>, string> LanguageListConverter = new(
        value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
        value => JsonSerializer.Deserialize<List<PreferredLanguage>>(value, (JsonSerializerOptions?)null) ?? new List<PreferredLanguage>());

    private static readonly ValueComparer<List<string>> StringListComparer = new(
        (left, right) => left != null && right != null ? left.SequenceEqual(right) : left == right,
        value => value.Aggregate(0, (hashCode, item) => HashCode.Combine(hashCode, StringComparer.Ordinal.GetHashCode(item))),
        value => value.ToList());

    public void Configure(EntityTypeBuilder<RecommendationProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("recommendation_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.MinimumAge);
        builder.Property(x => x.MaximumAge);
        builder.Property(x => x.FavoriteAuthors)
            .HasConversion(StringListConverter)
            .Metadata.SetValueComparer(StringListComparer);
        builder.Property(x => x.FavoriteGenres)
            .HasConversion(StringListConverter)
            .Metadata.SetValueComparer(StringListComparer);
        builder.Property(x => x.FavoriteStyles)
            .HasConversion(StringListConverter)
            .Metadata.SetValueComparer(StringListComparer);
        builder.Property(x => x.ExcludedAuthors)
            .HasConversion(StringListConverter)
            .Metadata.SetValueComparer(StringListComparer);
        builder.Property(x => x.ExcludedGenres)
            .HasConversion(StringListConverter)
            .Metadata.SetValueComparer(StringListComparer);
        builder.Property(x => x.ExcludedStyles)
            .HasConversion(StringListConverter)
            .Metadata.SetValueComparer(StringListComparer);
        builder.Property(x => x.PreferredBookLanguages)
            .HasConversion(LanguageListConverter)
            .Metadata.SetValueComparer(new ValueComparer<List<PreferredLanguage>>(
                (left, right) => left != null && right != null ? left.SequenceEqual(right) : left == right,
                value => value.Aggregate(0, (hashCode, item) => HashCode.Combine(hashCode, item.GetHashCode())),
                value => value.ToList()));
        builder.Property(x => x.PreferenceNotes).HasMaxLength(1000);
        builder.Property(x => x.ProfileVisibility).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MemberId).IsUnique();
    }
}

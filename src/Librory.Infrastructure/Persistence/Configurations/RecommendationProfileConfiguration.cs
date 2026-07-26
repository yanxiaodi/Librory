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

        builder.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MemberId).IsUnique();
    }
}

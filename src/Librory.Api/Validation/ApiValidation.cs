namespace Librory.Api.Validation;

internal sealed record ValidationField(string Key, string? Value, string Message);

internal static class ApiValidation
{
    public static IResult? Required(params ValidationField[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var errors = fields
            .Where(field => string.IsNullOrWhiteSpace(field.Value))
            .GroupBy(field => field.Key, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(field => field.Message).ToArray(),
                StringComparer.Ordinal);

        return errors.Count == 0
            ? null
            : Results.ValidationProblem(errors);
    }
}

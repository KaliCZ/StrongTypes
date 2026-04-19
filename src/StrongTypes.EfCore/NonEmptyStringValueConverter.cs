using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.EfCore;

/// <summary>
/// EF Core value converter that round-trips <see cref="NonEmptyString"/> through
/// a plain <see cref="string"/> column. EF Core applies this converter only to
/// non-null values, so the same instance works for both <c>NonEmptyString</c>
/// and <c>NonEmptyString?</c> properties.
/// </summary>
public sealed class NonEmptyStringValueConverter : ValueConverter<NonEmptyString, string>
{
    public NonEmptyStringValueConverter()
        : base(v => v.Value, v => NonEmptyString.Create(v))
    {
    }
}

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.EfCore;

/// <summary>EF Core value converter that round-trips <see cref="NonEmptyString"/> through a plain <see cref="string"/> column.</summary>
/// <remarks>The same instance serves both <c>NonEmptyString</c> and <c>NonEmptyString?</c> properties.</remarks>
public sealed class NonEmptyStringValueConverter : ValueConverter<NonEmptyString, string>
{
    public NonEmptyStringValueConverter()
        : base(v => v.Value, v => NonEmptyString.Create(v))
    {
    }
}

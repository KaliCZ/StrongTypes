using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.EfCore;

/// <summary>EF Core value converter that round-trips <see cref="Email"/> through a plain <see cref="string"/> column.</summary>
public sealed class EmailValueConverter : ValueConverter<Email, string>
{
    public EmailValueConverter()
        : base(v => v.Address, v => Email.Create(v))
    {
    }
}

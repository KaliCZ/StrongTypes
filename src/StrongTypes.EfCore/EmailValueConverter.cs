using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.EfCore;

/// <summary>EF Core value converter that round-trips <see cref="Email"/> through a plain <see cref="string"/> column; a stored value that no longer satisfies the email contract throws on read rather than materialising a broken wrapper.</summary>
/// <remarks>The same instance serves both <c>Email</c> and <c>Email?</c> properties.</remarks>
public sealed class EmailValueConverter : ValueConverter<Email, string>
{
    public EmailValueConverter()
        : base(v => v.Address, v => Email.Create(v))
    {
    }
}

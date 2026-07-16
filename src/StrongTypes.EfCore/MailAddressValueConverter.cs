using System.Net.Mail;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.EfCore;

/// <summary>EF Core value converter that round-trips <see cref="MailAddress"/> through a plain <see cref="string"/> column.</summary>
public sealed class MailAddressValueConverter : ValueConverter<MailAddress, string>
{
    public MailAddressValueConverter()
        : base(v => v.Address, v => new MailAddress(v))
    {
    }
}

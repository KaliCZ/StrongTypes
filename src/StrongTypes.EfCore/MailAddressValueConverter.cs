using System.Net.Mail;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.EfCore;

/// <summary>EF Core value converter that round-trips <see cref="MailAddress"/> through a plain <see cref="string"/> column. Strong-type validation belongs at the wire boundary; once an address has been accepted by the converter on the way in, the column stores the BCL primitive directly.</summary>
public sealed class MailAddressValueConverter : ValueConverter<MailAddress, string>
{
    public MailAddressValueConverter()
        : base(v => v.Address, v => new MailAddress(v))
    {
    }
}

#nullable enable

using System;
using System.Drawing;
using System.Windows.Forms;

namespace StrongTypes.WinForms.Tests;

/// <summary>
/// WinForms bindings stay dormant until their control sits on a shown form — parenting alone
/// or a force-created handle is not enough — so this shows the host form off-screen.
/// </summary>
internal sealed class HostedForm : IDisposable
{
    private readonly Form _form;

    public HostedForm(params Control[] controls)
    {
        _form = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-10000, -10000),
            ShowInTaskbar = false,
        };
        _form.Controls.AddRange(controls);
        _form.Show();
    }

    public void Dispose() => _form.Dispose();
}

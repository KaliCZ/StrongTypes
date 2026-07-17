#nullable enable

using System;
using System.Drawing;
using System.Windows.Forms;

namespace StrongTypes.WinForms.Tests;

/// <summary>
/// WinForms bindings stay dormant until their control lives on a created, visible host —
/// an unparented control, or a hidden Form with a force-created handle, never activates
/// them. Hosts controls on a Form shown off-screen so bindings run without a visible window.
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

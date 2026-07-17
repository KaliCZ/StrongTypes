#nullable enable

using System.Windows.Forms;

namespace StrongTypes.WinForms.Tests;

/// <summary>Records the first failed <see cref="Binding.BindingComplete"/> state — WinForms' error surface, where WPF raises a validation error.</summary>
internal sealed class BindingFailureRecorder
{
    public BindingCompleteState? State { get; private set; }

    public BindingFailureRecorder(Binding binding) =>
        binding.BindingComplete += (_, e) =>
        {
            if (e.BindingCompleteState != BindingCompleteState.Success)
            {
                State ??= e.BindingCompleteState;
            }
        };
}

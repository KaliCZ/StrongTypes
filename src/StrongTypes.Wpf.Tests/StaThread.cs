#nullable enable

using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace StrongTypes.Wpf.Tests;

/// <summary>Runs an action on a fresh thread set to Single-Threaded Apartment (STA) mode. WPF's <c>DependencyObject</c> infrastructure (and therefore every <c>TextBox</c>, <c>Binding</c>, etc. these tests construct) requires the calling thread to be STA; xUnit worker threads are Multi-Threaded Apartment by default, which would throw on <c>new TextBox()</c>.</summary>
internal static class StaThread
{
    public static void Run(Action body) => Run<object?>(() => { body(); return null; });

    public static T Run<T>(Func<T> body)
    {
        T result = default!;
        ExceptionDispatchInfo? captured = null;
        var thread = new Thread(() =>
        {
            try { result = body(); }
            catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();
        captured?.Throw();
        return result;
    }
}

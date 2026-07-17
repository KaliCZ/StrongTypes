#nullable enable

using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace StrongTypes.WinForms.Tests;

/// <summary>WinForms controls require an STA thread; xUnit worker threads are MTA.</summary>
internal static class StaThread
{
    public static void Run(Action body)
    {
        ExceptionDispatchInfo? captured = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();
        captured?.Throw();
    }
}

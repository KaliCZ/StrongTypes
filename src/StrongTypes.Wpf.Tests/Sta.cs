#nullable enable

using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace StrongTypes.Wpf.Tests;

internal static class Sta
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

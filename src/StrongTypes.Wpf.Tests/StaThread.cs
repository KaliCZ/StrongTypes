#nullable enable

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Threading;

namespace StrongTypes.Wpf.Tests;

/// <summary>WPF's <c>DependencyObject</c> infrastructure requires an STA thread; xUnit worker threads are MTA.</summary>
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
            // Constructing a WPF element spins up a Dispatcher for this thread; without shutdown it
            // survives the thread and the test host force-exits over the leftover foreground threads.
            finally { Dispatcher.FromThread(Thread.CurrentThread)?.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();
        captured?.Throw();
        return result;
    }
}

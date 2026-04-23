using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultThrowIfErrorTests
{
    // ── Default form: Result<T> (TError = Exception) ───────────────────

    [Property]
    public void ThrowIfError_Default_Success_ReturnsValue(int value)
    {
        Result<int> r = value;
        Assert.Equal(value, r.ThrowIfError());
    }

    [Fact]
    public void ThrowIfError_Default_Error_RethrowsExactException()
    {
        var original = new InvalidOperationException("boom");
        Result<int> r = original;
        var thrown = Assert.Throws<InvalidOperationException>(() => r.ThrowIfError());
        Assert.Same(original, thrown);
    }

    [Fact]
    public void ThrowIfError_Default_PreservesOriginalStackTrace()
    {
        // Capture a real stack trace by letting something throw, then re-wrap.
        Exception? captured = null;
        try { throw new InvalidOperationException("deep"); }
        catch (Exception e) { captured = e; }
        Result<int> r = captured!;

        var thrown = Assert.Throws<InvalidOperationException>(() => r.ThrowIfError());
        // The original frame must still be reachable in the rethrown stack.
        Assert.Contains(nameof(ThrowIfError_Default_PreservesOriginalStackTrace), thrown.StackTrace);
    }

    // ── Concrete-Exception TError: parameterless overload still applies ─

    [Fact]
    public void ThrowIfError_ConcreteExceptionTError_NoConverterNeeded()
    {
        var original = new InvalidOperationException("specific");
        Result<int, InvalidOperationException> r = original;
        var thrown = Assert.Throws<InvalidOperationException>(() => r.ThrowIfError());
        Assert.Same(original, thrown);
    }

    [Property]
    public void ThrowIfError_ConcreteExceptionTError_Success_ReturnsValue(int value)
    {
        Result<int, InvalidOperationException> r = value;
        Assert.Equal(value, r.ThrowIfError());
    }

    // ── Custom TError form ────────────────────────────────────────────

    [Property]
    public void ThrowIfError_Custom_Success_ReturnsValue(int value)
    {
        Result<int, string> r = value;
        Assert.Equal(value, r.ThrowIfError(e => new InvalidOperationException(e)));
    }

    [Property]
    public void ThrowIfError_Custom_Error_InvokesConverterAndThrows(string errorMessage)
    {
        Result<int, string> r = errorMessage;
        var thrown = Assert.Throws<InvalidOperationException>(() =>
            r.ThrowIfError(e => new InvalidOperationException(e)));
        Assert.Equal(errorMessage, thrown.Message);
    }

    // ── Aggregate form: Result<T, IReadOnlyList<Exception>> ────────────

    [Property]
    public void ThrowIfError_Aggregate_Success_ReturnsValue(int value)
    {
        // Implicit conversion from the int success value works because T is a
        // concrete type; the TError interface branch of the implicit operator
        // doesn't apply (C# forbids interface source types), so the aggregate
        // error case constructs via the factory instead.
        Result<int, IReadOnlyList<Exception>> r = value;
        Assert.Equal(value, r.ThrowIfError());
    }

    [Fact]
    public void ThrowIfError_Aggregate_SingleError_RethrowsDirectly()
    {
        var inner = new InvalidOperationException("only one");
        IReadOnlyList<Exception> errors = new[] { inner };
        var r = Result.Error<int, IReadOnlyList<Exception>>(errors);
        var thrown = Assert.Throws<InvalidOperationException>(() => r.ThrowIfError());
        Assert.Same(inner, thrown);
    }

    [Fact]
    public void ThrowIfError_Aggregate_MultipleErrors_WrapsInAggregateException()
    {
        IReadOnlyList<Exception> errors = new Exception[]
        {
            new InvalidOperationException("a"),
            new ArgumentException("b"),
        };
        var r = Result.Error<int, IReadOnlyList<Exception>>(errors);
        var thrown = Assert.Throws<AggregateException>(() => r.ThrowIfError());
        Assert.Equal(errors, thrown.InnerExceptions.ToArray());
    }

    // ── Overload resolution sanity ─────────────────────────────────────

    [Fact]
    public void ParameterlessOverload_ChosenOnResultOfT_NoConverterNeeded()
    {
        // Compile-time check: calling ThrowIfError() with no args on a
        // Result<int> must bind to the default overload, not the generic one
        // (which would require a Func<TError, Exception>).
        Result<int> r = 42;
        int value = r.ThrowIfError();
        Assert.Equal(42, value);
    }
}

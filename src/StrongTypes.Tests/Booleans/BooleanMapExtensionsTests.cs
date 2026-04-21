#nullable enable

using System;
using System.Threading.Tasks;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class BooleanMapExtensionsTests
{
    // ── MapTrue — struct ──────────────────────────────────────────────

    [Property]
    public void MapTrue_Struct_TrueInvokesMap(int raw)
    {
        int? result = true.MapTrue(() => raw);
        Assert.Equal(raw, result);
    }

    [Fact]
    public void MapTrue_Struct_FalseReturnsNullWithoutInvoking()
    {
        var invoked = false;
        int? result = false.MapTrue(() => { invoked = true; return 1; });
        Assert.Null(result);
        Assert.False(invoked);
    }

    // ── MapTrue — class ───────────────────────────────────────────────

    [Property]
    public void MapTrue_Class_TrueInvokesMap(NonEmptyString raw)
    {
        string? result = true.MapTrue(() => raw.Value);
        Assert.Equal(raw.Value, result);
    }

    [Fact]
    public void MapTrue_Class_FalseReturnsNullWithoutInvoking()
    {
        var invoked = false;
        string? result = false.MapTrue(() => { invoked = true; return "x"; });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public void MapTrue_Class_MapperMayReturnNull()
    {
        string? result = true.MapTrue(() => (string?)null);
        Assert.Null(result);
    }

    // ── MapFalse — struct ─────────────────────────────────────────────

    [Property]
    public void MapFalse_Struct_FalseInvokesMap(int raw)
    {
        int? result = false.MapFalse(() => raw);
        Assert.Equal(raw, result);
    }

    [Fact]
    public void MapFalse_Struct_TrueReturnsNullWithoutInvoking()
    {
        var invoked = false;
        int? result = true.MapFalse(() => { invoked = true; return 1; });
        Assert.Null(result);
        Assert.False(invoked);
    }

    // ── MapFalse — class ──────────────────────────────────────────────

    [Property]
    public void MapFalse_Class_FalseInvokesMap(NonEmptyString raw)
    {
        string? result = false.MapFalse(() => raw.Value);
        Assert.Equal(raw.Value, result);
    }

    [Fact]
    public void MapFalse_Class_TrueReturnsNullWithoutInvoking()
    {
        var invoked = false;
        string? result = true.MapFalse(() => { invoked = true; return "x"; });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public void MapFalse_Class_MapperMayReturnNull()
    {
        string? result = false.MapFalse(() => (string?)null);
        Assert.Null(result);
    }

    // ── MapTrueAsync — struct ─────────────────────────────────────────

    [Fact]
    public async Task MapTrueAsync_Struct_TrueAwaitsAndReturns()
    {
        int? result = await true.MapTrueAsync(async () => { await Task.Yield(); return 7; });
        Assert.Equal(7, result);
    }

    [Fact]
    public async Task MapTrueAsync_Struct_FalseReturnsNullWithoutInvoking()
    {
        var invoked = false;
        int? result = await false.MapTrueAsync(async () =>
        {
            invoked = true;
            await Task.Yield();
            return 7;
        });
        Assert.Null(result);
        Assert.False(invoked);
    }

    // ── MapTrueAsync — class ──────────────────────────────────────────

    [Fact]
    public async Task MapTrueAsync_Class_TrueAwaitsAndReturns()
    {
        string? result = await true.MapTrueAsync(async () => { await Task.Yield(); return "x"; });
        Assert.Equal("x", result);
    }

    [Fact]
    public async Task MapTrueAsync_Class_FalseReturnsNullWithoutInvoking()
    {
        var invoked = false;
        string? result = await false.MapTrueAsync(async () =>
        {
            invoked = true;
            await Task.Yield();
            return "x";
        });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public async Task MapTrueAsync_Class_MapperMayReturnNull()
    {
        string? result = await true.MapTrueAsync(async () =>
        {
            await Task.Yield();
            return (string?)null;
        });
        Assert.Null(result);
    }

    // ── MapFalseAsync — struct ────────────────────────────────────────

    [Fact]
    public async Task MapFalseAsync_Struct_FalseAwaitsAndReturns()
    {
        int? result = await false.MapFalseAsync(async () => { await Task.Yield(); return 9; });
        Assert.Equal(9, result);
    }

    [Fact]
    public async Task MapFalseAsync_Struct_TrueReturnsNullWithoutInvoking()
    {
        var invoked = false;
        int? result = await true.MapFalseAsync(async () =>
        {
            invoked = true;
            await Task.Yield();
            return 9;
        });
        Assert.Null(result);
        Assert.False(invoked);
    }

    // ── MapFalseAsync — class ─────────────────────────────────────────

    [Fact]
    public async Task MapFalseAsync_Class_FalseAwaitsAndReturns()
    {
        string? result = await false.MapFalseAsync(async () => { await Task.Yield(); return "y"; });
        Assert.Equal("y", result);
    }

    [Fact]
    public async Task MapFalseAsync_Class_TrueReturnsNullWithoutInvoking()
    {
        var invoked = false;
        string? result = await true.MapFalseAsync(async () =>
        {
            invoked = true;
            await Task.Yield();
            return "y";
        });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public async Task MapFalseAsync_Class_MapperMayReturnNull()
    {
        string? result = await false.MapFalseAsync(async () =>
        {
            await Task.Yield();
            return (string?)null;
        });
        Assert.Null(result);
    }
}

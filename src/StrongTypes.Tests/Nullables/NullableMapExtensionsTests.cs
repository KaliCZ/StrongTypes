using System;
using System.Threading.Tasks;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NullableMapExtensionsTests
{
    // ── struct → struct ────────────────────────────────────────────────

    [Property]
    public void Map_StructToStruct_SomeMaps(int raw)
    {
        int? value = raw;
        int? result = value.Map(x => x + 1);
        Assert.Equal(raw + 1, result);
    }

    [Fact]
    public void Map_StructToStruct_NullReturnsNull()
    {
        int? value = null;
        var invoked = false;
        int? result = value.Map(x => { invoked = true; return x + 1; });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public void Map_StructToStruct_MapperMayReturnNull()
    {
        int? value = 5;
        int? result = value.Map(x => (int?)null);
        Assert.Null(result);
    }

    [Fact]
    public void Map_StructToStruct_NullableReturningMapperPropagates()
    {
        static int? Parse(string s) => int.TryParse(s, out var n) ? n : null;
        int? len = 3;
        int? result = len.Map(n => Parse(new string('1', n)));
        Assert.Equal(111, result);
    }

    // ── struct → class ────────────────────────────────────────────────

    [Property]
    public void Map_StructToClass_SomeMaps(int raw)
    {
        int? value = raw;
        string? result = value.Map(x => x.ToString());
        Assert.Equal(raw.ToString(), result);
    }

    [Fact]
    public void Map_StructToClass_NullReturnsNull()
    {
        int? value = null;
        var invoked = false;
        string? result = value.Map(x => { invoked = true; return x.ToString(); });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public void Map_StructToClass_MapperMayReturnNull()
    {
        int? value = 5;
        string? result = value.Map(_ => (string?)null);
        Assert.Null(result);
    }

    // ── class → struct ────────────────────────────────────────────────

    [Property]
    public void Map_ClassToStruct_SomeMaps(NonEmptyString value)
    {
        string? s = value.Value;
        int? result = s.Map(x => x.Length);
        Assert.Equal(value.Value.Length, result);
    }

    [Fact]
    public void Map_ClassToStruct_NullReturnsNull()
    {
        string? value = null;
        var invoked = false;
        int? result = value.Map(x => { invoked = true; return x.Length; });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public void Map_ClassToStruct_MapperMayReturnNull()
    {
        string? value = "hi";
        int? result = value.Map(x => (int?)null);
        Assert.Null(result);
    }

    [Fact]
    public void Map_ClassToStruct_NullableReturningMapperPropagates()
    {
        static int? Parse(string s) => int.TryParse(s, out var n) ? n : null;
        string? s = "42";
        int? result = s.Map(Parse);
        Assert.Equal(42, result);
    }

    // ── class → class ────────────────────────────────────────────────

    [Property]
    public void Map_ClassToClass_SomeMaps(NonEmptyString value)
    {
        string? s = value.Value;
        string? result = s.Map(x => x + "!");
        Assert.Equal(value.Value + "!", result);
    }

    [Fact]
    public void Map_ClassToClass_NullReturnsNull()
    {
        string? value = null;
        var invoked = false;
        string? result = value.Map(x => { invoked = true; return x + "!"; });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public void Map_ClassToClass_MapperMayReturnNull()
    {
        string? value = "hi";
        string? result = value.Map(_ => (string?)null);
        Assert.Null(result);
    }

    // ── MapAsync — struct → struct ────────────────────────────────────

    [Fact]
    public async Task MapAsync_StructToStruct_SomeAwaitsAndMaps()
    {
        int? value = 3;
        int? result = await value.MapAsync(async x => { await Task.Yield(); return x + 1; });
        Assert.Equal(4, result);
    }

    [Fact]
    public async Task MapAsync_StructToStruct_NullReturnsNullWithoutInvoking()
    {
        int? value = null;
        var invoked = false;
        int? result = await value.MapAsync(async x =>
        {
            invoked = true;
            await Task.Yield();
            return x + 1;
        });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public async Task MapAsync_StructToStruct_MapperMayReturnNull()
    {
        int? value = 3;
        int? result = await value.MapAsync(async _ => { await Task.Yield(); return (int?)null; });
        Assert.Null(result);
    }

    // ── MapAsync — struct → class ─────────────────────────────────────

    [Fact]
    public async Task MapAsync_StructToClass_SomeAwaitsAndMaps()
    {
        int? value = 3;
        string? result = await value.MapAsync(async x => { await Task.Yield(); return x.ToString(); });
        Assert.Equal("3", result);
    }

    [Fact]
    public async Task MapAsync_StructToClass_NullReturnsNullWithoutInvoking()
    {
        int? value = null;
        var invoked = false;
        string? result = await value.MapAsync(async x =>
        {
            invoked = true;
            await Task.Yield();
            return x.ToString();
        });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public async Task MapAsync_StructToClass_MapperMayReturnNull()
    {
        int? value = 3;
        string? result = await value.MapAsync(async _ => { await Task.Yield(); return (string?)null; });
        Assert.Null(result);
    }

    // ── MapAsync — class → struct ─────────────────────────────────────

    [Fact]
    public async Task MapAsync_ClassToStruct_SomeAwaitsAndMaps()
    {
        string? value = "abc";
        int? result = await value.MapAsync(async x => { await Task.Yield(); return x.Length; });
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task MapAsync_ClassToStruct_NullReturnsNullWithoutInvoking()
    {
        string? value = null;
        var invoked = false;
        int? result = await value.MapAsync(async x =>
        {
            invoked = true;
            await Task.Yield();
            return x.Length;
        });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public async Task MapAsync_ClassToStruct_MapperMayReturnNull()
    {
        string? value = "abc";
        int? result = await value.MapAsync(async _ => { await Task.Yield(); return (int?)null; });
        Assert.Null(result);
    }

    // ── MapAsync — class → class ──────────────────────────────────────

    [Fact]
    public async Task MapAsync_ClassToClass_SomeAwaitsAndMaps()
    {
        string? value = "hi";
        string? result = await value.MapAsync(async x => { await Task.Yield(); return x + "!"; });
        Assert.Equal("hi!", result);
    }

    [Fact]
    public async Task MapAsync_ClassToClass_NullReturnsNullWithoutInvoking()
    {
        string? value = null;
        var invoked = false;
        string? result = await value.MapAsync(async x =>
        {
            invoked = true;
            await Task.Yield();
            return x + "!";
        });
        Assert.Null(result);
        Assert.False(invoked);
    }

    [Fact]
    public async Task MapAsync_ClassToClass_MapperMayReturnNull()
    {
        string? value = "hi";
        string? result = await value.MapAsync(async _ => { await Task.Yield(); return (string?)null; });
        Assert.Null(result);
    }
}

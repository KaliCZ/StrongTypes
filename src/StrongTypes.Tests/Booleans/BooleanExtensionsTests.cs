using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class BooleanExtensionsTests
{
    [Property]
    public void Implies_Eager_MatchesTruthTable(bool a, bool b) =>
        Assert.Equal(!a || b, a.Implies(b));

    [Property]
    public void Implies_Lazy_MatchesTruthTable(bool a, bool b) =>
        Assert.Equal(!a || b, a.Implies(() => b));

    [Fact]
    public void Implies_FalseAntecedent_ShortCircuits()
    {
        var called = 0;
        var result = false.Implies(() => { called++; return true; });
        Assert.True(result);
        Assert.Equal(0, called);
    }

    [Fact]
    public void Implies_TrueAntecedent_InvokesConsequent()
    {
        var called = 0;
        var result = true.Implies(() => { called++; return false; });
        Assert.False(result);
        Assert.Equal(1, called);
    }
}

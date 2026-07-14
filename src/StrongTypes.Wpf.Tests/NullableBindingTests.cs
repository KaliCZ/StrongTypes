#nullable enable

using System.Windows.Controls;
using System.Windows.Data;
using StrongTypes.Wpf.TestApp;
using Xunit;
using static StrongTypes.Wpf.Tests.Bindings;

namespace StrongTypes.Wpf.Tests;

/// <summary>
/// The two nullable shapes resolve their converter differently — a nullable struct wrapper goes
/// through the BCL's <c>NullableConverter</c>, which we do not own, while a nullable reference
/// wrapper hits our converter directly because nullability is erased at runtime. Both are pinned
/// here because neither is obvious from the property declaration.
/// </summary>
public class NullableStructBindingTests
{
    [Fact]
    public void OneWay_Null_DisplaysEmpty()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Score = null };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, OneWay(nameof(vm.Score), vm));

            Assert.Equal("", textBox.Text);
        });
    }

    [Fact]
    public void OneWay_Value_DisplaysIt()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Score = Positive<int>.Create(10) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, OneWay(nameof(vm.Score), vm));

            Assert.Equal("10", textBox.Text);
        });
    }

    [Fact]
    public void TwoWay_ValidInput_UpdatesSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Score = null };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Score), vm));

            textBox.Text = "10";

            Assert.Equal(Positive<int>.Create(10), vm.Score);
        });
    }

    /// <summary>
    /// Clearing the box does <b>not</b> null the source. WPF unwraps <c>Nullable&lt;T&gt;</c> and asks
    /// <c>TypeDescriptor</c> for the underlying type's converter, so the BCL's <c>NullableConverter</c>
    /// — which would map empty to null, and does for configuration binding — is never consulted;
    /// <c>""</c> reaches <c>Positive&lt;int&gt;.Parse</c> and fails. Surprising, but long-standing:
    /// no converter has ever been registered against <c>Nullable&lt;T&gt;</c> here.
    /// </summary>
    [Fact]
    public void TwoWay_ClearedInput_RaisesValidationErrorRatherThanNulling()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Score = Positive<int>.Create(10) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Score), vm));

            textBox.Text = "";

            Assert.Equal(Positive<int>.Create(10), vm.Score);
            Assert.True(Validation.GetHasError(textBox));
        });
    }

    /// <summary>Nullable does not mean unvalidated: a present value still has to satisfy the invariant.</summary>
    [Fact]
    public void TwoWay_InvalidInput_DoesNotMutateSourceAndRaisesValidationError()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Score = Positive<int>.Create(10) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Score), vm));

            textBox.Text = "-1";

            Assert.Equal(Positive<int>.Create(10), vm.Score);
            Assert.True(Validation.GetHasError(textBox));
        });
    }
}

public class NullableReferenceBindingTests
{
    [Fact]
    public void OneWay_Null_DisplaysEmpty()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Nickname = null };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, OneWay(nameof(vm.Nickname), vm));

            Assert.Equal("", textBox.Text);
        });
    }

    [Fact]
    public void TwoWay_ValidInput_UpdatesSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Nickname = null };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Nickname), vm));

            textBox.Text = "Ally";

            Assert.Equal(NonEmptyString.Create("Ally"), vm.Nickname);
        });
    }

    [Fact]
    public void TwoWay_InvalidInput_DoesNotMutateSourceAndRaisesValidationError()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Nickname = NonEmptyString.Create("Ally") };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Nickname), vm));

            textBox.Text = "   ";

            Assert.Equal(NonEmptyString.Create("Ally"), vm.Nickname);
            Assert.True(Validation.GetHasError(textBox));
        });
    }
}

#nullable enable

using System.Windows.Controls;
using System.Windows.Data;
using StrongTypes.Wpf.TestApp;
using Xunit;
using static StrongTypes.Wpf.Tests.Bindings;

namespace StrongTypes.Wpf.Tests;

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

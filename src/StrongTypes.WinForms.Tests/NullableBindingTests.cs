#nullable enable

using System.Windows.Forms;
using Xunit;
using static StrongTypes.WinForms.Tests.Bindings;

namespace StrongTypes.WinForms.Tests;

public class NullableStructBindingTests
{
    [Fact]
    public void Hosted_Null_DisplaysEmpty()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Score = null };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Score), vm));
            using var form = new HostedForm(textBox);

            Assert.Equal("", textBox.Text);
        });
    }

    [Fact]
    public void TwoWay_ValidInput_UpdatesSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Score = null };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Score), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "10";

            Assert.Equal(Positive<int>.Create(10), vm.Score);
        });
    }

    [Fact]
    public void TwoWay_ClearedInput_KeepsTheLastValueRatherThanNulling()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Score = Positive<int>.Create(10) };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Score), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "";

            Assert.Equal(Positive<int>.Create(10), vm.Score);
        });
    }

    /// <summary>Nullable does not mean unvalidated: a present value still has to satisfy the invariant.</summary>
    [Fact]
    public void TwoWay_InvalidInput_DoesNotMutateSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Score = Positive<int>.Create(10) };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Score), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "-1";

            Assert.Equal(Positive<int>.Create(10), vm.Score);
        });
    }
}

public class NullableReferenceBindingTests
{
    [Fact]
    public void Hosted_Null_DisplaysEmpty()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Nickname = null };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Nickname), vm));
            using var form = new HostedForm(textBox);

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
            textBox.DataBindings.Add(TwoWay(nameof(vm.Nickname), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "Ally";

            Assert.Equal(NonEmptyString.Create("Ally"), vm.Nickname);
        });
    }

    [Fact]
    public void TwoWay_WhitespaceInput_DoesNotMutateSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Nickname = NonEmptyString.Create("Ally") };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Nickname), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "   ";

            Assert.Equal(NonEmptyString.Create("Ally"), vm.Nickname);
        });
    }
}

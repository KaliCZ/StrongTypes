#nullable enable

using System.Windows.Forms;
using Xunit;
using static StrongTypes.WinForms.Tests.Bindings;

namespace StrongTypes.WinForms.Tests;

public class NonEmptyStringBindingTests
{
    [Fact]
    public void Hosted_DisplaysCurrentValue()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Name = NonEmptyString.Create("Alice") };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Name), vm));
            using var form = new HostedForm(textBox);

            Assert.Equal("Alice", textBox.Text);
        });
    }

    [Fact]
    public void Unhosted_BindingStaysDormant()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Name = NonEmptyString.Create("Alice") };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Name), vm));

            Assert.Equal("", textBox.Text);
        });
    }

    [Fact]
    public void SourceChange_ReflectsInControl()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Name = NonEmptyString.Create("Alice") };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Name), vm));
            using var form = new HostedForm(textBox);

            vm.Name = NonEmptyString.Create("Bob");

            Assert.Equal("Bob", textBox.Text);
        });
    }

    [Fact]
    public void TwoWay_ValidInput_UpdatesSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Name = NonEmptyString.Create("Alice") };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Name), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "Bob";

            Assert.Equal(NonEmptyString.Create("Bob"), vm.Name);
        });
    }

    [Fact]
    public void TwoWay_InvalidInput_DoesNotMutateSourceAndReportsBindingError()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Name = NonEmptyString.Create("Alice") };
            var textBox = new TextBox();
            var binding = TwoWay(nameof(vm.Name), vm);
            BindingCompleteState? failure = null;
            binding.BindingComplete += (_, e) =>
            {
                if (e.BindingCompleteState != BindingCompleteState.Success)
                {
                    failure = e.BindingCompleteState;
                }
            };
            textBox.DataBindings.Add(binding);
            using var form = new HostedForm(textBox);

            textBox.Text = "   ";

            Assert.Equal(NonEmptyString.Create("Alice"), vm.Name);
            Assert.Equal(BindingCompleteState.Exception, failure);
        });
    }
}

public class EmailBindingTests
{
    [Fact]
    public void Hosted_DisplaysAddress()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Email = Email.Create("alice@example.com") };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Email), vm));
            using var form = new HostedForm(textBox);

            Assert.Equal("alice@example.com", textBox.Text);
        });
    }

    [Fact]
    public void TwoWay_ValidInput_UpdatesSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel();
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Email), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "bob@example.com";

            Assert.Equal(Email.Create("bob@example.com"), vm.Email);
        });
    }

    [Fact]
    public void TwoWay_InvalidInput_DoesNotMutateSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Email = Email.Create("alice@example.com") };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Email), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "not an email";

            Assert.Equal(Email.Create("alice@example.com"), vm.Email);
        });
    }
}

public class PositiveIntBindingTests
{
    [Fact]
    public void Hosted_DisplaysCurrentValue()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Age = Positive<int>.Create(30) };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Age), vm));
            using var form = new HostedForm(textBox);

            Assert.Equal("30", textBox.Text);
        });
    }

    [Fact]
    public void TwoWay_ValidInput_UpdatesSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Age = Positive<int>.Create(30) };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Age), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "42";

            Assert.Equal(Positive<int>.Create(42), vm.Age);
        });
    }

    [Fact]
    public void TwoWay_InvariantBreach_DoesNotMutateSourceAndRecovers()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Age = Positive<int>.Create(30) };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Age), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "0";
            Assert.Equal(Positive<int>.Create(30), vm.Age);

            textBox.Text = "7";
            Assert.Equal(Positive<int>.Create(7), vm.Age);
        });
    }

    [Fact]
    public void TwoWay_NonNumericInput_DoesNotMutateSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Age = Positive<int>.Create(30) };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Age), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "abc";

            Assert.Equal(Positive<int>.Create(30), vm.Age);
        });
    }
}

public class DigitBindingTests
{
    [Fact]
    public void Hosted_DisplaysCurrentValue()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Tier = Digit.Create('7') };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Tier), vm));
            using var form = new HostedForm(textBox);

            Assert.Equal("7", textBox.Text);
        });
    }

    [Fact]
    public void TwoWay_ValidInput_UpdatesSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Tier = Digit.Create('7') };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Tier), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "3";

            Assert.Equal(Digit.Create('3'), vm.Tier);
        });
    }

    [Fact]
    public void TwoWay_InvalidInput_DoesNotMutateSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Tier = Digit.Create('7') };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Tier), vm));
            using var form = new HostedForm(textBox);

            textBox.Text = "42";

            Assert.Equal(Digit.Create('7'), vm.Tier);
        });
    }
}

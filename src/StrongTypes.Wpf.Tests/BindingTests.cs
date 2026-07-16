#nullable enable

using System.Windows.Controls;
using System.Windows.Data;
using StrongTypes.Wpf.TestApp;
using Xunit;
using static StrongTypes.Wpf.Tests.Bindings;

namespace StrongTypes.Wpf.Tests;

public class NonEmptyStringBindingTests
{
    [Fact]
    public void OneWay_DisplaysCurrentValue()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Name = NonEmptyString.Create("Alice") };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, OneWay(nameof(vm.Name), vm));

            Assert.Equal("Alice", textBox.Text);
        });
    }

    [Fact]
    public void OneWay_ReflectsSourceChange()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Name = NonEmptyString.Create("Alice") };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, OneWay(nameof(vm.Name), vm));

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
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Name), vm));

            textBox.Text = "Bob";

            Assert.Equal(NonEmptyString.Create("Bob"), vm.Name);
        });
    }

    [Fact]
    public void TwoWay_InvalidInput_DoesNotMutateSourceAndRaisesValidationError()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Name = NonEmptyString.Create("Alice") };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Name), vm));

            textBox.Text = "   ";

            Assert.Equal(NonEmptyString.Create("Alice"), vm.Name);
            Assert.True(Validation.GetHasError(textBox));
        });
    }
}

public class EmailBindingTests
{
    [Fact]
    public void OneWay_DisplaysAddress()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Email = Email.Create("alice@example.com") };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, OneWay(nameof(vm.Email), vm));

            Assert.Equal("alice@example.com", textBox.Text);
        });
    }

    [Fact]
    public void TwoWay_ValidInput_UpdatesSource()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Email = Email.Create("alice@example.com") };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Email), vm));

            textBox.Text = "bob@example.com";

            Assert.Equal(Email.Create("bob@example.com"), vm.Email);
        });
    }

    [Fact]
    public void TwoWay_InvalidInput_DoesNotMutateSourceAndRaisesValidationError()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Email = Email.Create("alice@example.com") };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Email), vm));

            textBox.Text = "not-an-email";

            Assert.Equal(Email.Create("alice@example.com"), vm.Email);
            Assert.True(Validation.GetHasError(textBox));
        });
    }
}

public class PositiveIntBindingTests
{
    [Fact]
    public void OneWay_DisplaysCurrentValue()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Age = Positive<int>.Create(30) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, OneWay(nameof(vm.Age), vm));

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
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Age), vm));

            textBox.Text = "42";

            Assert.Equal(Positive<int>.Create(42), vm.Age);
        });
    }

    [Fact]
    public void TwoWay_InvalidInput_DoesNotMutateSourceAndRaisesValidationError()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Age = Positive<int>.Create(30) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Age), vm));

            textBox.Text = "0";

            Assert.Equal(Positive<int>.Create(30), vm.Age);
            Assert.True(Validation.GetHasError(textBox));
        });
    }

    [Fact]
    public void TwoWay_NonNumericInput_DoesNotMutateSourceAndRaisesValidationError()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Age = Positive<int>.Create(30) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Age), vm));

            textBox.Text = "abc";

            Assert.Equal(Positive<int>.Create(30), vm.Age);
            Assert.True(Validation.GetHasError(textBox));
        });
    }
}

public class DigitBindingTests
{
    [Fact]
    public void OneWay_DisplaysCurrentValue()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Tier = Digit.Create('7') };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, OneWay(nameof(vm.Tier), vm));

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
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Tier), vm));

            textBox.Text = "3";

            Assert.Equal(Digit.Create('3'), vm.Tier);
        });
    }

    [Fact]
    public void TwoWay_InvalidInput_DoesNotMutateSourceAndRaisesValidationError()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Tier = Digit.Create('7') };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Tier), vm));

            textBox.Text = "42";

            Assert.Equal(Digit.Create('7'), vm.Tier);
            Assert.True(Validation.GetHasError(textBox));
        });
    }
}

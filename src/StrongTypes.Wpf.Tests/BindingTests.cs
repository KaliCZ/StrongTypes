#nullable enable

using System.Windows.Controls;
using System.Windows.Data;
using StrongTypes.Wpf.TestApp;
using Xunit;

namespace StrongTypes.Wpf.Tests;

public class NonEmptyStringBindingTests
{
    [Fact]
    public void OneWay_DisplaysCurrentValue()
    {
        Sta.Run(() =>
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
        Sta.Run(() =>
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
        Sta.Run(() =>
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
        Sta.Run(() =>
        {
            var vm = new PersonViewModel { Name = NonEmptyString.Create("Alice") };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Name), vm));

            textBox.Text = "   ";

            Assert.Equal(NonEmptyString.Create("Alice"), vm.Name);
            Assert.True(System.Windows.Controls.Validation.GetHasError(textBox));
        });
    }

    private static Binding OneWay(string path, object source) => new(path)
    {
        Source = source,
        Mode = BindingMode.OneWay,
    };

    private static Binding TwoWay(string path, object source) => new(path)
    {
        Source = source,
        Mode = BindingMode.TwoWay,
        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        ValidatesOnExceptions = true,
        NotifyOnValidationError = true,
    };
}

public class EmailBindingTests
{
    [Fact]
    public void OneWay_DisplaysAddress()
    {
        Sta.Run(() =>
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
        Sta.Run(() =>
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
        Sta.Run(() =>
        {
            var vm = new PersonViewModel { Email = Email.Create("alice@example.com") };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Email), vm));

            textBox.Text = "not-an-email";

            Assert.Equal(Email.Create("alice@example.com"), vm.Email);
            Assert.True(System.Windows.Controls.Validation.GetHasError(textBox));
        });
    }

    private static Binding OneWay(string path, object source) => new(path)
    {
        Source = source,
        Mode = BindingMode.OneWay,
    };

    private static Binding TwoWay(string path, object source) => new(path)
    {
        Source = source,
        Mode = BindingMode.TwoWay,
        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        ValidatesOnExceptions = true,
        NotifyOnValidationError = true,
    };
}

public class PositiveIntBindingTests
{
    [Fact]
    public void OneWay_DisplaysCurrentValue()
    {
        Sta.Run(() =>
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
        Sta.Run(() =>
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
        Sta.Run(() =>
        {
            var vm = new PersonViewModel { Age = Positive<int>.Create(30) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Age), vm));

            textBox.Text = "0";

            Assert.Equal(Positive<int>.Create(30), vm.Age);
            Assert.True(System.Windows.Controls.Validation.GetHasError(textBox));
        });
    }

    [Fact]
    public void TwoWay_NonNumericInput_DoesNotMutateSourceAndRaisesValidationError()
    {
        Sta.Run(() =>
        {
            var vm = new PersonViewModel { Age = Positive<int>.Create(30) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Age), vm));

            textBox.Text = "abc";

            Assert.Equal(Positive<int>.Create(30), vm.Age);
            Assert.True(System.Windows.Controls.Validation.GetHasError(textBox));
        });
    }

    private static Binding OneWay(string path, object source) => new(path)
    {
        Source = source,
        Mode = BindingMode.OneWay,
    };

    private static Binding TwoWay(string path, object source) => new(path)
    {
        Source = source,
        Mode = BindingMode.TwoWay,
        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        ValidatesOnExceptions = true,
        NotifyOnValidationError = true,
    };
}

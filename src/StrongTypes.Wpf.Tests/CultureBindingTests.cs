#nullable enable

using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using StrongTypes.Wpf.TestApp;
using Xunit;
using static StrongTypes.Wpf.Tests.Bindings;

namespace StrongTypes.Wpf.Tests;

/// <summary>
/// A binding hands the converter one culture for both directions, so display and write-back must
/// agree on it. The converter this replaced formatted with the ambient culture while parsing in the
/// binding's: on a de-DE binding it rendered 1234.5 as "1234.5", which reads back as 12345.
/// </summary>
public class CultureBindingTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    [Fact]
    public void OneWay_FormatsInTheBindingCulture()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm, German));

            Assert.Equal("1234,5", textBox.Text);
        });
    }

    [Fact]
    public void TwoWay_ParsesInTheBindingCulture()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm, German));

            textBox.Text = "9876,5";

            Assert.Equal(Positive<decimal>.Create(9876.5m), vm.Salary);
        });
    }

    /// <summary>Display then re-commit unchanged text must be a no-op; when the two directions disagree on culture it silently multiplies the value by 10.</summary>
    [Fact]
    public void DisplayedTextRoundTripsBackToTheSameValue()
    {
        StaThread.Run(() =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm, German));

            textBox.Text = textBox.Text;

            Assert.Equal(Positive<decimal>.Create(1234.5m), vm.Salary);
            Assert.False(Validation.GetHasError(textBox));
        });
    }
}

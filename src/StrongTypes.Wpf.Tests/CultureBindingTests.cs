#nullable enable

using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using StrongTypes.Wpf.TestApp;
using Xunit;
using static StrongTypes.Wpf.Tests.Bindings;

namespace StrongTypes.Wpf.Tests;

/// <summary>
/// A binding hands the converter one culture for both directions, so display and write-back must
/// agree on it — and on the binding's culture, not the host's.
/// </summary>
public class CultureBindingTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Pinned, and deliberately not a decimal-comma culture: a converter that formatted with the
    /// ambient culture instead of the binding's would still pass every assertion below on a
    /// de-DE — or cs-CZ — host, because the two separators happen to agree.
    /// </summary>
    private static readonly CultureInfo Host = CultureInfo.GetCultureInfo("en-US");

    private static void RunOnAmericanHost(Action body) => StaThread.Run(() =>
    {
        CultureInfo.CurrentCulture = Host;
        body();
    });

    [Fact]
    public void OneWay_FormatsInTheBindingCultureNotTheHostCulture() =>
        RunOnAmericanHost(() =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm, German));

            Assert.Equal("1234,5", textBox.Text);
        });

    [Fact]
    public void TwoWay_ParsesInTheBindingCulture() =>
        RunOnAmericanHost(() =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm, German));

            textBox.Text = "9876,5";

            Assert.Equal(Positive<decimal>.Create(9876.5m), vm.Salary);
        });

    /// <summary>
    /// Committing the displayed text unedited must be a no-op. When the directions disagree on
    /// culture it is not: the box shows "1234.5", de-DE reads the dot as a group separator, and the
    /// view model silently becomes 12345.
    /// </summary>
    [Fact]
    public void DisplayedTextRoundTripsBackToTheSameValue() =>
        RunOnAmericanHost(() =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm, German));

            textBox.Text = textBox.Text;

            Assert.Equal(Positive<decimal>.Create(1234.5m), vm.Salary);
            Assert.False(Validation.GetHasError(textBox));
        });

    /// <summary>The host's own culture still drives a binding that does not name one.</summary>
    [Fact]
    public void NoConverterCulture_FormatsInTheHostCulture() =>
        RunOnAmericanHost(() =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox();
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm));

            Assert.Equal("1234.5", textBox.Text);
        });
}

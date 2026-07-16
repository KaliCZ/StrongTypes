#nullable enable

using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using StrongTypes.Wpf.TestApp;
using Xunit;
using static StrongTypes.Wpf.Tests.Bindings;

namespace StrongTypes.Wpf.Tests;

/// <summary>Display and write-back both use the binding's culture, not the host's.</summary>
public class CultureBindingTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Pinned to a non-decimal-comma culture on purpose: a converter that formatted with the host
    /// culture instead of the binding's would still pass every assertion below on a de-DE host.
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

    /// <summary>Committing the displayed text unedited must be a no-op.</summary>
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

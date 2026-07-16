#nullable enable

using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using StrongTypes.Wpf.TestApp;
using Xunit;
using static StrongTypes.Wpf.Tests.Bindings;

namespace StrongTypes.Wpf.Tests;

/// <summary>
/// WPF resolves a binding's culture as <c>ConverterCulture ?? element.Language</c> and never consults
/// the host thread culture. Each case therefore runs under every host culture in turn, asserting the
/// host is ignored while the binding culture governs display, write-back, and round-trip.
/// </summary>
public class CultureBindingTests
{
    private static readonly string[] HostCultures = ["en-US", "de-DE", "cs-CZ", "ja-JP"];

    [Theory]
    [InlineData("en-US", 1234.5, "1234.5")]
    [InlineData("de-DE", 1234.5, "1234,5")]
    [InlineData("cs-CZ", 1234.5, "1234,5")]
    [InlineData("ja-JP", 1234.5, "1234.5")]
    public void DisplaysAndRoundTripsInTheBindingCulture(string cultureName, double value, string expectedText)
    {
        var binding = CultureInfo.GetCultureInfo(cultureName);
        var salary = Positive<decimal>.Create((decimal)value);
        RunUnderEveryHost(host =>
        {
            var vm = new PersonViewModel { Salary = salary };
            var textBox = BoundWithConverterCulture(vm, binding);

            Assert.Equal(expectedText, textBox.Text);

            textBox.Text = textBox.Text;

            Assert.False(Validation.GetHasError(textBox), $"host {host}: committing the displayed text raised an error");
            Assert.Equal(salary, vm.Salary);
        });
    }

    /// <summary>
    /// A separator from the wrong culture is silently swallowed as digit grouping — <c>"9876,5"</c> under
    /// en-US binds to <c>98765</c>, a valid value and no error — so the culture must match the input.
    /// </summary>
    [Theory]
    [InlineData("en-US", "9876.5", 9876.5)]
    [InlineData("de-DE", "9876,5", 9876.5)]
    [InlineData("cs-CZ", "9876,5", 9876.5)]
    [InlineData("ja-JP", "9876.5", 9876.5)]
    [InlineData("en-US", "9876,5", 98765.0)]
    [InlineData("de-DE", "9876.5", 98765.0)]
    [InlineData("en-US", "not-a-number", null)]
    [InlineData("de-DE", "not-a-number", null)]
    [InlineData("en-US", "-5", null)]
    [InlineData("de-DE", "-5", null)]
    public void WriteBackParsesInTheBindingCulture(string cultureName, string text, double? expected)
    {
        var binding = CultureInfo.GetCultureInfo(cultureName);
        RunUnderEveryHost(host =>
        {
            var original = Positive<decimal>.Create(1m);
            var vm = new PersonViewModel { Salary = original };
            var textBox = BoundWithConverterCulture(vm, binding);

            textBox.Text = text;

            if (expected is { } number)
            {
                Assert.False(Validation.GetHasError(textBox), $"host {host}: valid input '{text}' raised an error");
                Assert.Equal(Positive<decimal>.Create((decimal)number), vm.Salary);
            }
            else
            {
                Assert.True(Validation.GetHasError(textBox), $"host {host}: invalid input '{text}' was accepted");
                Assert.Equal(original, vm.Salary);
            }
        });
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("cs-CZ")]
    [InlineData("ja-JP")]
    public void NoConverterCulture_usesTheElementLanguageNotTheHost(string languageName)
    {
        var language = CultureInfo.GetCultureInfo(languageName);
        RunUnderEveryHost(host =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox { Language = XmlLanguage.GetLanguage(languageName) };
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm));

            Assert.Equal(1234.5m.ToString(language), textBox.Text);
        });
    }

    private static void RunUnderEveryHost(Action<string> body)
    {
        foreach (var host in HostCultures)
        {
            StaThread.Run(() =>
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(host);
                body(host);
            });
        }
    }

    private static TextBox BoundWithConverterCulture(PersonViewModel vm, CultureInfo converterCulture)
    {
        var textBox = new TextBox();
        BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm, converterCulture));
        return textBox;
    }
}

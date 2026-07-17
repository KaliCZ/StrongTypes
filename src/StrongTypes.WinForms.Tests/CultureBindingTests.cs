#nullable enable

using System;
using System.Globalization;
using System.Windows.Forms;
using Xunit;
using static StrongTypes.WinForms.Tests.Bindings;

namespace StrongTypes.WinForms.Tests;

/// <summary>
/// WinForms resolves a binding's culture as <c>FormatInfo ?? CultureInfo.CurrentCulture</c> —
/// unlike WPF, the host culture governs by default. The <c>FormatInfo</c> cases therefore run
/// under every host culture in turn, asserting an explicit culture makes the host irrelevant.
/// </summary>
public class CultureBindingTests
{
    private static readonly string[] HostCultures = ["en-US", "de-DE", "cs-CZ", "ja-JP"];

    [Theory]
    [InlineData("en-US", "1234.5")]
    [InlineData("de-DE", "1234,5")]
    [InlineData("cs-CZ", "1234,5")]
    [InlineData("ja-JP", "1234.5")]
    public void FormatInfo_DisplaysAndRoundTripsRegardlessOfHost(string cultureName, string expectedText)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var salary = Positive<decimal>.Create(1234.5m);
        RunUnderEveryHost(host =>
        {
            var vm = new PersonViewModel { Salary = salary };
            var textBox = new TextBox();
            var binding = TwoWay(nameof(vm.Salary), vm, culture);
            var failure = new BindingFailureRecorder(binding);
            textBox.DataBindings.Add(binding);
            using var form = new HostedForm(textBox);

            Assert.Equal(expectedText, textBox.Text);

            binding.WriteValue();

            Assert.True(failure.State is null, $"host {host}: committing the displayed text raised a binding error");
            Assert.Equal(salary, vm.Salary);
        });
    }

    /// <summary>
    /// A separator from the wrong culture is silently swallowed as digit grouping — <c>"9876,5"</c>
    /// under en-US binds to <c>98765</c>, a valid value and no error — so the culture must match the input.
    /// </summary>
    [Theory]
    [InlineData("en-US", "9876.5", "9876.5")]
    [InlineData("de-DE", "9876,5", "9876.5")]
    [InlineData("cs-CZ", "9876,5", "9876.5")]
    [InlineData("en-US", "9876,5", "98765")]
    [InlineData("de-DE", "9876.5", "98765")]
    [InlineData("en-US", "not-a-number", null)]
    [InlineData("de-DE", "not-a-number", null)]
    [InlineData("en-US", "-5", null)]
    [InlineData("de-DE", "-5", null)]
    public void FormatInfo_GovernsWriteBackRegardlessOfHost(string cultureName, string text, string? expected)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        RunUnderEveryHost(host =>
        {
            var original = Positive<decimal>.Create(1m);
            var vm = new PersonViewModel { Salary = original };
            var textBox = new TextBox();
            var binding = TwoWay(nameof(vm.Salary), vm, culture);
            var failure = new BindingFailureRecorder(binding);
            textBox.DataBindings.Add(binding);
            using var form = new HostedForm(textBox);

            textBox.Text = text;

            if (expected is { } number)
            {
                Assert.True(failure.State is null, $"host {host}: valid input '{text}' raised a binding error");
                Assert.Equal(Positive<decimal>.Create(decimal.Parse(number, CultureInfo.InvariantCulture)), vm.Salary);
            }
            else
            {
                Assert.Equal(BindingCompleteState.Exception, failure.State);
                Assert.Equal(original, vm.Salary);
            }
        });
    }

    [Theory]
    [InlineData("en-US", "1234.5")]
    [InlineData("de-DE", "1234,5")]
    [InlineData("cs-CZ", "1234,5")]
    public void NoFormatInfo_UsesTheHostCulture(string hostName, string expectedText)
    {
        StaThread.Run(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(hostName);
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Salary), vm));
            using var form = new HostedForm(textBox);

            Assert.Equal(expectedText, textBox.Text);
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
}

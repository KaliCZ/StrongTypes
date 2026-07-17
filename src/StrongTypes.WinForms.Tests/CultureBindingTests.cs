#nullable enable

using System;
using System.Globalization;
using System.Windows.Forms;
using Xunit;
using static StrongTypes.WinForms.Tests.Bindings;

namespace StrongTypes.WinForms.Tests;

/// <summary>
/// WinForms resolves a binding's culture as <c>FormatInfo ?? CultureInfo.CurrentCulture</c> —
/// unlike WPF, the host culture governs by default. The <c>FormatInfo</c> cases run under every
/// host culture in turn to prove an explicit culture makes the host irrelevant; the
/// no-<c>FormatInfo</c> cases pin the host-culture default.
/// </summary>
public class CultureBindingTests
{
    private static readonly string[] HostCultures = ["en-US", "de-DE", "cs-CZ", "ja-JP"];

    [Theory]
    [InlineData("en-US", "1234.5")]
    [InlineData("de-DE", "1234,5")]
    [InlineData("cs-CZ", "1234,5")]
    [InlineData("ja-JP", "1234.5")]
    public void FormatInfo_GovernsDisplayRegardlessOfHost(string cultureName, string expectedText)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        RunUnderEveryHost(host =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Salary), vm, culture));
            using var form = new HostedForm(textBox);

            Assert.Equal(expectedText, textBox.Text);
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
    public void FormatInfo_GovernsWriteBackRegardlessOfHost(string cultureName, string text, string expected)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var expectedSalary = Positive<decimal>.Create(decimal.Parse(expected, CultureInfo.InvariantCulture));
        RunUnderEveryHost(host =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1m) };
            var textBox = new TextBox();
            textBox.DataBindings.Add(TwoWay(nameof(vm.Salary), vm, culture));
            using var form = new HostedForm(textBox);

            textBox.Text = text;

            Assert.Equal(expectedSalary, vm.Salary);
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

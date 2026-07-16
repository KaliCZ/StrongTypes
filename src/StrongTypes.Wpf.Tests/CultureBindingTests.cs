#nullable enable

using System;
using System.Collections.Generic;
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
/// the host thread culture — so every case runs the same value through a matrix of host thread
/// cultures crossed with binding cultures, proving the host is ignored.
/// </summary>
public class CultureBindingTests
{
    private static readonly string[] Cultures = ["en-US", "de-DE", "cs-CZ", "ja-JP"];

    public static IEnumerable<object[]> HostAndBinding()
    {
        foreach (var host in Cultures)
        {
            foreach (var binding in Cultures)
            {
                yield return [host, binding];
            }
        }
    }

    [Theory]
    [MemberData(nameof(HostAndBinding))]
    public void Display_usesTheConverterCulture(string hostName, string bindingName)
    {
        var binding = CultureInfo.GetCultureInfo(bindingName);
        RunOn(hostName, () =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = BoundWithConverterCulture(vm, binding);

            Assert.Equal(1234.5m.ToString(binding), textBox.Text);
        });
    }

    [Theory]
    [MemberData(nameof(HostAndBinding))]
    public void WriteBack_parsesInTheConverterCulture(string hostName, string bindingName)
    {
        var binding = CultureInfo.GetCultureInfo(bindingName);
        RunOn(hostName, () =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = BoundWithConverterCulture(vm, binding);

            textBox.Text = 9876.5m.ToString(binding);

            Assert.Equal(Positive<decimal>.Create(9876.5m), vm.Salary);
        });
    }

    /// <summary>Committing the displayed text unedited must not corrupt the value — the bug that shipped when the converter formatted in the host culture but parsed in the binding culture.</summary>
    [Theory]
    [MemberData(nameof(HostAndBinding))]
    public void RoundTrip_committingDisplayedTextIsANoOp(string hostName, string bindingName)
    {
        var binding = CultureInfo.GetCultureInfo(bindingName);
        RunOn(hostName, () =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = BoundWithConverterCulture(vm, binding);

            textBox.Text = textBox.Text;

            Assert.Equal(Positive<decimal>.Create(1234.5m), vm.Salary);
            Assert.False(Validation.GetHasError(textBox));
        });
    }

    /// <summary>With no ConverterCulture the binding falls back to the element's Language, still never the host.</summary>
    [Theory]
    [MemberData(nameof(HostAndBinding))]
    public void NoConverterCulture_usesTheElementLanguageNotTheHost(string hostName, string languageName)
    {
        var language = CultureInfo.GetCultureInfo(languageName);
        RunOn(hostName, () =>
        {
            var vm = new PersonViewModel { Salary = Positive<decimal>.Create(1234.5m) };
            var textBox = new TextBox { Language = XmlLanguage.GetLanguage(languageName) };
            BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm));

            Assert.Equal(1234.5m.ToString(language), textBox.Text);
        });
    }

    private static void RunOn(string cultureName, Action body) => StaThread.Run(() =>
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
        body();
    });

    private static TextBox BoundWithConverterCulture(PersonViewModel vm, CultureInfo converterCulture)
    {
        var textBox = new TextBox();
        BindingOperations.SetBinding(textBox, TextBox.TextProperty, TwoWay(nameof(vm.Salary), vm, converterCulture));
        return textBox;
    }
}

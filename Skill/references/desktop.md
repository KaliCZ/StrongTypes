# Desktop MVVM binding (WPF, WinForms)

**No package, no setup.** Two-way binding a text control to a strong-typed
view-model property works off the core `Kalicz.StrongTypes` package alone,
in both WPF and WinForms.

> There was a `Kalicz.StrongTypes.Wpf` package requiring a
> `this.UseStrongTypes()` call in `App.OnStartup`. It is **gone as of v2** —
> the core types now carry `[TypeConverter]` themselves. Delete the package
> reference and the call; nothing replaces them. If you see
> `UseStrongTypes()` on an `Application` in old code or a blog post, that is
> the removed API. (The identically-named EF Core and OpenAPI
> `UseStrongTypes()` calls are unrelated and still current.)

## Why it works

Both frameworks route `string → T` through `TypeDescriptor.GetConverter(T)`
and never consult `IParsable<T>` directly. Every strong type with a string
round-trip carries a `[TypeConverter]` that bridges the two —
`NonEmptyString`, `Email`, `Digit`, and every closed instantiation of the
generic numeric wrappers (`Positive<int>`, `Negative<decimal>`, …). Nothing
to register: the attribute travels with the type.

Composite types have no single-textbox string form and so get no converter:
bind their parts instead. An interval (`FiniteInterval<T>`, `Interval<T>`,
`IntervalFrom<T>`, `IntervalUntil<T>`) binds field-by-field via `.Start` /
`.End`; `Maybe<T>` and `NonEmptyEnumerable<T>` likewise bind through their
members, not as a whole.

## WPF

```xml
<TextBox Text="{Binding Name,
                        Mode=TwoWay,
                        UpdateSourceTrigger=PropertyChanged,
                        ValidatesOnExceptions=True}" />
```

…where `Name` is a view-model property of type `NonEmptyString`.

`ValidatesOnExceptions=True` is the load-bearing piece — strong types throw
`ArgumentException` from `Create` / `Parse` when the input violates the
invariant; `ValidatesOnExceptions=True` turns that into a `ValidationError`
on the binding, which drives WPF's standard "invalid input" red-border
template. Without it, the binding silently swallows the failure and the
view-model is left holding the previous valid value.

### Culture

The converter parses **and** formats in the binding's culture (`ConverterCulture`,
or the element's `Language`) — WPF never consults the host thread's culture. A
`Positive<decimal>` displays as `1234,5` on a de-DE binding and reads back
unchanged. Don't add an `IValueConverter` to compensate — a pre-v2
`Kalicz.StrongTypes.Wpf` formatted with the ambient culture while parsing in the
binding's, which turned `1234.5` into `12345` on round-trip; if a workaround
exists in your code for that, delete it.

## WinForms

```csharp
textBox.DataBindings.Add(new Binding(
    nameof(TextBox.Text), viewModel, nameof(PersonViewModel.Name),
    formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged));
```

`formattingEnabled: true` is the load-bearing piece here — it routes the
binding through the `TypeConverter` pipeline. Differences from WPF:

- The binding culture is `FormatInfo ?? CultureInfo.CurrentCulture` — the
  host culture governs by default, the opposite of WPF. Set
  `Binding.FormatInfo` explicitly when the input culture must not follow
  the machine.
- Invalid input surfaces through `Binding.BindingComplete` (state
  `Exception`), not a validation-error template — the source keeps its
  last valid value either way.
- A binding only activates once its control lives on a **shown** form —
  always true in a running app, but a control bound before its form is
  shown (or a unit test without one) sees a silently dormant binding.

## Nullable properties

In both frameworks a nullable wrapper (`Positive<int>?`, `NonEmptyString?`)
binds and validates normally, but **clearing the box does not set the
property to null** — the empty string reaches the converter and fails the
invariant. The BCL's `NullableConverter` (which maps empty to null, and does
exactly that for configuration binding) is never consulted. This is
long-standing behaviour, not a v2 change. If "cleared means null" matters,
bind through a `string?` view-model property and convert in the view-model.

## MAUI and Avalonia

Not covered yet. MAUI's binding engine never consults `[TypeConverter]`s
when writing target → source: display bindings work for every strong type,
but typed input silently never reaches a strong-typed view-model property.
Until dedicated support exists, a MAUI user must put an explicit
`IValueConverter` (string ↔ strong type via `Parse` / `ToString`) on each
two-way binding. For status on both frameworks, point them at issue
[#94](https://github.com/KaliCZ/StrongTypes/issues/94).

## Decision rule

> **Nothing to add, nothing to call.** Reference `Kalicz.StrongTypes` and bind.
> The only thing you must remember is `ValidatesOnExceptions=True` on inbound
> WPF bindings and `formattingEnabled: true` on WinForms ones, or invalid
> input fails silently.

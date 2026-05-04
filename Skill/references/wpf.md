# WPF MVVM binding — `Kalicz.StrongTypes.Wpf`

Adds the `TypeConverter` infrastructure WPF needs to two-way bind a
`TextBox` (or any other input control) to a strong-typed view-model
property.

## Why a separate package

WPF's binding pipeline routes `string → T` through
`TypeDescriptor.GetConverter(T)` and never consults `IParsable<T>`
directly. Without a converter, typing into a `TextBox` bound to e.g.
a `NonEmptyString` view-model property silently fails to update the
source. The core `Kalicz.StrongTypes` package deliberately carries no
UI dependency, so the wiring lives here.

## Wiring

Call `this.UseStrongTypes()` once in `App.OnStartup`. One call covers
every strong type, including every closed instantiation of the
generic numeric wrappers (`Positive<int>`, `Negative<decimal>`, …) —
the package installs a `TypeDescriptionProvider` that synthesises the
converter on demand the first time WPF asks for it.

```csharp
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        this.UseStrongTypes();
        base.OnStartup(e);
    }
}
```

## Bindings — what to write in XAML

After registration, plain MVVM bindings to strong-typed properties
just work:

```xml
<TextBox Text="{Binding Name,
                        Mode=TwoWay,
                        UpdateSourceTrigger=PropertyChanged,
                        ValidatesOnExceptions=True}" />
```

…where `Name` is a view-model property of type `NonEmptyString`.

`ValidatesOnExceptions=True` is the load-bearing piece — strong
types throw `ArgumentException` from `Create` / `Parse` when the
input violates the invariant; `ValidatesOnExceptions=True` turns
that into a `ValidationError` on the binding, which drives WPF's
standard "invalid input" red-border template. Without it, the
binding silently swallows the failure and the view-model is left
holding the previous valid value.

## Other XAML/MVVM frameworks

The same `TypeDescriptor`-based mechanism is used by WinForms and
some other frameworks; in practice this package is currently
documented and tested for WPF. If a user is on Avalonia, MAUI, or
WinForms and hits the same "binding silently fails" symptom, point
them at issue
[#94](https://github.com/KaliCZ/StrongTypes/issues/94).

## Decision rule

> **Use it whenever a WPF view-model property is a strong type that
> the user can type into.** Display-only bindings (one-way out) work
> without it because WPF goes through `ToString()`. The package
> matters for the inbound (string → T) leg.

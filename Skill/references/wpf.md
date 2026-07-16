# WPF MVVM binding

**No package, no setup.** Two-way binding a `TextBox` to a strong-typed
view-model property works off the core `Kalicz.StrongTypes` package alone.

> There was a `Kalicz.StrongTypes.Wpf` package requiring a
> `this.UseStrongTypes()` call in `App.OnStartup`. It is **gone as of v2** —
> the core types now carry `[TypeConverter]` themselves. Delete the package
> reference and the call; nothing replaces them. If you see
> `UseStrongTypes()` on an `Application` in old code or a blog post, that is
> the removed API. (The identically-named EF Core and OpenAPI
> `UseStrongTypes()` calls are unrelated and still current.)

## Why it works

WPF's binding pipeline routes `string → T` through
`TypeDescriptor.GetConverter(T)` and never consults `IParsable<T>` directly.
Every strong type with a string round-trip carries a `[TypeConverter]` that
bridges the two — `NonEmptyString`, `Email`, `Digit`, and every closed
instantiation of the generic numeric wrappers (`Positive<int>`,
`Negative<decimal>`, …). Nothing to register: the attribute travels with the
type.

Composite types have no single-`TextBox` string form and so get no converter:
bind their parts instead. An interval (`FiniteInterval<T>`, `Interval<T>`,
`IntervalFrom<T>`, `IntervalUntil<T>`) binds field-by-field via `.Start` /
`.End`; `Maybe<T>` and `NonEmptyEnumerable<T>` likewise bind through their
members, not as a whole.

## Bindings — what to write in XAML

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

## Culture

The converter parses **and** formats in the binding's culture (`ConverterCulture`,
or the element's `Language`), so a `Positive<decimal>` displays as `1234,5` on a
de-DE binding and reads back unchanged. Don't add an `IValueConverter` to
compensate — a pre-v2 `Kalicz.StrongTypes.Wpf` formatted with the ambient
culture while parsing in the binding's, which turned `1234.5` into `12345` on
round-trip; if a workaround exists in your code for that, delete it.

## Nullable properties

A nullable wrapper (`Positive<int>?`, `NonEmptyString?`) binds and validates
normally, but **clearing the box does not set the property to null** — it
raises a validation error. WPF unwraps `Nullable<T>` and asks for the
underlying type's converter, so the BCL's `NullableConverter` (which maps empty
to null, and does exactly that for configuration binding) is never consulted,
and `""` reaches `Positive<int>.Parse`. This is long-standing behaviour, not a
v2 change. If "cleared means null" matters, bind through a `string?` view-model
property and convert in the view-model.

## Other XAML/MVVM frameworks

The same `TypeDescriptor` mechanism is used by WinForms and some other
frameworks, and since the converters now live on the types themselves, nothing
WPF-specific is required for them either. In practice this is documented and
tested for WPF. If a user is on Avalonia, MAUI, or WinForms and hits a
"binding silently fails" symptom, point them at issue
[#94](https://github.com/KaliCZ/StrongTypes/issues/94).

## Decision rule

> **Nothing to add, nothing to call.** Reference `Kalicz.StrongTypes` and bind.
> The only thing you must remember is `ValidatesOnExceptions=True` on inbound
> (string → T) bindings, or invalid input fails silently.

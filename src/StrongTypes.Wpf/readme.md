# Kalicz.StrongTypes.Wpf

`TypeConverter`s that bridge `Kalicz.StrongTypes`' `IParsable<T>` implementations into WPF — and into any other framework (WinForms, XAML islands, …) that resolves converters through `TypeDescriptor.GetConverter`. With this package referenced and `Register()` called, two-way binding from a `TextBox.Text` to a strong-typed view-model property just works.

## Why a separate package

WPF's binding pipeline routes `string → T` through `TypeDescriptor.GetConverter(T)` and never consults `IParsable<T>` directly. Without a converter, typing into a `TextBox` bound to a strong-typed property silently fails to update the source. The core `Kalicz.StrongTypes` package deliberately avoids any UI dependency, so the wiring lives here.

## Usage

Call `StrongTypesWpf.Register()` once at application startup. For generic numeric wrappers, register each closed instantiation explicitly:

```csharp
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        StrongTypesWpf.Register();
        StrongTypesWpf.Register<Positive<int>>();
        base.OnStartup(e);
    }
}
```

After registration, a plain MVVM binding works:

```xml
<TextBox Text="{Binding Name,
                        Mode=TwoWay,
                        UpdateSourceTrigger=PropertyChanged,
                        ValidatesOnExceptions=True}" />
```

…where `Name` is a view-model property of type `NonEmptyString`. `ValidatesOnExceptions=True` turns the strong type's `ArgumentException` (thrown by `Create` / `Parse` when validation fails) into a `ValidationError` on the binding, which in turn drives the standard WPF "invalid input" red border.

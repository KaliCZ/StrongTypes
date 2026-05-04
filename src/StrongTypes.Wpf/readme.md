# Kalicz.StrongTypes.Wpf

[![NuGet version](https://img.shields.io/nuget/v/Kalicz.StrongTypes.Wpf?label=nuget)](https://www.nuget.org/packages/Kalicz.StrongTypes.Wpf/) [![Downloads](https://img.shields.io/nuget/dt/Kalicz.StrongTypes.Wpf?label=downloads)](https://www.nuget.org/packages/Kalicz.StrongTypes.Wpf/) [![License](https://img.shields.io/github/license/KaliCZ/StrongTypes)](https://github.com/KaliCZ/StrongTypes/blob/main/license.txt)

WPF MVVM binding support for [Kalicz.StrongTypes](https://www.nuget.org/packages/Kalicz.StrongTypes). With this package referenced and `UseStrongTypes()` called once in `App.OnStartup`, two-way binding from a `TextBox.Text` to a strong-typed view-model property just works.

## Why a separate package

WPF's binding pipeline routes `string → T` through `TypeDescriptor.GetConverter(T)` and never consults `IParsable<T>` directly. Without a converter, typing into a `TextBox` bound to a strong-typed property silently fails to update the source. The core `Kalicz.StrongTypes` package deliberately avoids any UI dependency, so the wiring lives here.

## Usage

Call `this.UseStrongTypes()` once in `App.OnStartup`. One call covers every strong type — including every closed instantiation of the generic numeric wrappers (`Positive<int>`, `Negative<decimal>`, …) — because the package installs a `TypeDescriptionProvider` that synthesizes the converter on demand the first time WPF asks for it.

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

After registration, a plain MVVM binding works:

```xml
<TextBox Text="{Binding Name,
                        Mode=TwoWay,
                        UpdateSourceTrigger=PropertyChanged,
                        ValidatesOnExceptions=True}" />
```

…where `Name` is a view-model property of type `NonEmptyString`. `ValidatesOnExceptions=True` turns the strong type's `ArgumentException` (thrown by `Create` / `Parse` when validation fails) into a `ValidationError` on the binding, which in turn drives the standard WPF "invalid input" red border.

For non-WPF callers (tests, console hosts) where there is no `Application` to extend, call `StrongTypesWpf.Register()` directly — it does the same wiring. To register a custom `IParsable<T>` outside the StrongTypes library, call `StrongTypesWpf.Register<T>()`.

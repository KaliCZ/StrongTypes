#nullable enable

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StrongTypes.Wpf.TestApp;

public sealed class PersonViewModel : INotifyPropertyChanged
{
    private NonEmptyString _name = NonEmptyString.Create("Alice");
    private Email _email = Email.Create("alice@example.com");
    private Positive<int> _age = Positive<int>.Create(30);

    public NonEmptyString Name
    {
        get => _name;
        set { _name = value; Raise(); }
    }

    public Email Email
    {
        get => _email;
        set { _email = value; Raise(); }
    }

    public Positive<int> Age
    {
        get => _age;
        set { _age = value; Raise(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

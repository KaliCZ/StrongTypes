#nullable enable

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StrongTypes.WinForms.Tests;

public sealed class PersonViewModel : INotifyPropertyChanged
{
    private NonEmptyString _name = NonEmptyString.Create("Alice");
    private Email _email = Email.Create("alice@example.com");
    private Positive<int> _age = Positive<int>.Create(30);
    private Digit _tier = Digit.Create('7');
    private Positive<decimal> _salary = Positive<decimal>.Create(1234.5m);
    private NonEmptyString? _nickname;
    private Positive<int>? _score;

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

    public Digit Tier
    {
        get => _tier;
        set { _tier = value; Raise(); }
    }

    public Positive<decimal> Salary
    {
        get => _salary;
        set { _salary = value; Raise(); }
    }

    public NonEmptyString? Nickname
    {
        get => _nickname;
        set { _nickname = value; Raise(); }
    }

    public Positive<int>? Score
    {
        get => _score;
        set { _score = value; Raise(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

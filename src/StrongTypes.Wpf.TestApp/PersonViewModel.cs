#nullable enable

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StrongTypes.Wpf.TestApp;

/// <summary>Witness giving <see cref="BoundedInt{TBounds}"/> a 1..100 page-size range.</summary>
public readonly struct PageSizeBounds : IBounds<int>
{
    public static int Min => 1;
    public static int Max => 100;
}

public sealed class PersonViewModel : INotifyPropertyChanged
{
    private NonEmptyString _name = NonEmptyString.Create("Alice");
    private Email _email = Email.Create("alice@example.com");
    private Positive<int> _age = Positive<int>.Create(30);
    private BoundedInt<PageSizeBounds> _pageSize = BoundedInt<PageSizeBounds>.Create(20);

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

    public BoundedInt<PageSizeBounds> PageSize
    {
        get => _pageSize;
        set { _pageSize = value; Raise(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

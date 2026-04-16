using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.Api.Infrastructure;

/// <summary>
/// Generic EF Core value converter for numeric strong-type wrappers
/// (<see cref="Positive{T}"/>, <see cref="NonNegative{T}"/>, <see cref="Negative{T}"/>,
/// <see cref="NonPositive{T}"/>). Maps the wrapper to its underlying <typeparamref name="T"/>
/// column via the <c>Value</c> property and <c>Create</c> factory.
/// </summary>
public sealed class NumericStrongTypeValueConverter<TWrapper, T> : ValueConverter<TWrapper, T>
    where TWrapper : struct
    where T : struct
{
    public NumericStrongTypeValueConverter()
        : base(BuildToProvider(), BuildToModel())
    {
    }

    private static Expression<Func<TWrapper, T>> BuildToProvider()
    {
        var param = Expression.Parameter(typeof(TWrapper), "v");
        var value = Expression.Property(param, nameof(Positive<int>.Value));
        return Expression.Lambda<Func<TWrapper, T>>(value, param);
    }

    private static Expression<Func<T, TWrapper>> BuildToModel()
    {
        var param = Expression.Parameter(typeof(T), "v");
        var create = typeof(TWrapper).GetMethod(
            "Create", BindingFlags.Public | BindingFlags.Static, [typeof(T)])!;
        var call = Expression.Call(create, param);
        return Expression.Lambda<Func<T, TWrapper>>(call, param);
    }
}

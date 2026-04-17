using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrongTypes.EfCore;

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
    // Expression trees are compiled once per (TWrapper, T) pair and reused by
    // every ValueConverter instance. EF Core keeps converter instances around
    // for the lifetime of the model, but the model is sometimes rebuilt
    // (different DbContext types, design-time tooling), so caching here costs
    // one JIT per (TWrapper, T) instead of one per ValueConverter ctor call.
    private static readonly Expression<Func<TWrapper, T>> s_toProvider = BuildToProvider();
    private static readonly Expression<Func<T, TWrapper>> s_toModel = BuildToModel();

    public NumericStrongTypeValueConverter()
        : base(s_toProvider, s_toModel)
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

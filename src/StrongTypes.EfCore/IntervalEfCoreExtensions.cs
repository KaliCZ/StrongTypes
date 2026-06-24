using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StrongTypes.EfCore;

/// <summary>EF Core configuration helper for the four interval types: <see cref="ClosedInterval{T}"/>, <see cref="Interval{T}"/>, <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/>. Maps an interval property to a single JSON-encoded string column via <see cref="HasIntervalJsonConversion{TEntity,TInterval}"/>, round-tripping through the type's validating JSON converter. (EF Core's <c>ComplexProperty</c> mapping does not apply: the interval's private constructor cannot be bound as a complex type, and column-by-column materialization would bypass the <c>Start &lt;= End</c> invariant the converter enforces.)</summary>
public static class IntervalEfCoreExtensions
{
    /// <summary>Maps the interval property to a single JSON string column. The same converter handles all four interval types.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TInterval">The interval struct type.</typeparam>
    /// <param name="entity">The entity-type builder.</param>
    /// <param name="propertyExpression">A lambda selecting the interval property.</param>
    public static PropertyBuilder<TInterval> HasIntervalJsonConversion<TEntity, TInterval>(
        this EntityTypeBuilder<TEntity> entity,
        Expression<Func<TEntity, TInterval>> propertyExpression)
        where TEntity : class
        where TInterval : struct =>
        entity.Property(propertyExpression).HasConversion(new IntervalJsonValueConverter<TInterval>());
}

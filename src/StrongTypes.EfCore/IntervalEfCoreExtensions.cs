using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StrongTypes.EfCore;

/// <summary>EF Core configuration helpers for the four interval types: <see cref="ClosedInterval{T}"/>, <see cref="Interval{T}"/>, <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/>. Two persistence shapes are supported: a single JSON-encoded string column via <see cref="HasIntervalJsonConversion{TEntity,TInterval}"/>, or two scalar columns via EF Core's built-in <c>ComplexProperty</c> mapping (<c>entity.ComplexProperty(e =&gt; e.Interval)</c>) — the interval's <c>Start</c> and <c>End</c> become two columns whose nullability follows the interval variant.</summary>
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

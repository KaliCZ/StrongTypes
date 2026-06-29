using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StrongTypes.EfCore;

/// <summary>EF Core configuration helpers for the four interval types — <see cref="ClosedInterval{T}"/>, <see cref="Interval{T}"/>, <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/> — offering both persistence shapes:
/// <list type="bullet">
/// <item><see cref="HasIntervalJsonConversion{TEntity,TInterval}"/> — one JSON string column, round-tripped through the type's validating JSON converter (re-checks <c>Start &lt;= End</c> on read). This is the <b>default</b> shape: with <c>UseStrongTypes()</c> every interval property is auto-mapped this way, so calling it by hand is only needed when not using the convention.</item>
/// <item><see cref="HasIntervalColumns{TEntity,TInterval}"/> — two scalar columns (one per endpoint, nullability following the variant), mapped as an EF Core complex type. Opts out of the JSON default per property. Materializes directly from the columns, so it does <b>not</b> re-validate on read — the database is trusted as the source of truth.</item>
/// </list>
/// </summary>
public static class IntervalEfCoreExtensions
{
    /// <summary>Maps the interval property to a single JSON string column. The same converter handles all four interval types and re-validates the <c>Start &lt;= End</c> invariant on read.</summary>
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

    /// <summary>Maps the interval property to two scalar columns (<c>Start</c> and <c>End</c>) as an EF Core complex type, so each endpoint is its own queryable, indexable column. The endpoint columns are nullable exactly when the variant's endpoint is (<c>ClosedInterval</c> → both required; <c>IntervalFrom</c> → end nullable; and so on). Unlike the JSON shape, materialization reads the columns directly and does not re-check <c>Start &lt;= End</c>.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TInterval">The interval struct type.</typeparam>
    /// <param name="entity">The entity-type builder.</param>
    /// <param name="propertyExpression">A lambda selecting the interval property.</param>
    public static ComplexPropertyBuilder<TInterval> HasIntervalColumns<TEntity, TInterval>(
        this EntityTypeBuilder<TEntity> entity,
        Expression<Func<TEntity, TInterval>> propertyExpression)
        where TEntity : class
        where TInterval : struct =>
        entity.ComplexProperty(propertyExpression);
}

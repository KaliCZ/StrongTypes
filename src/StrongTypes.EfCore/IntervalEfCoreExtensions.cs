using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StrongTypes.EfCore;

/// <summary>EF Core configuration helpers for the four interval types — <see cref="FiniteInterval{T}"/>, <see cref="Interval{T}"/>, <see cref="IntervalFrom{T}"/>, <see cref="IntervalUntil{T}"/> — offering both persistence shapes:
/// <list type="bullet">
/// <item><see cref="HasIntervalColumns{TEntity,TInterval}(EntityTypeBuilder{TEntity},Expression{Func{TEntity,TInterval}},IntervalBoundMode,IntervalBoundMode)"/> — two scalar columns (one per endpoint, nullability following the variant), mapped as an EF Core complex type so endpoint access in LINQ translates to a plain, indexable column reference. This is the <b>default</b> shape: with <c>UseStrongTypes()</c> every interval property is auto-mapped this way, so calling it by hand is only needed when not using the convention, to configure the endpoint columns, or to pick an <see cref="IntervalBoundMode"/> per bound.</item>
/// <item><see cref="HasIntervalJsonConversion{TEntity,TInterval}"/> — one JSON column, round-tripped through the type's validating JSON converter, with endpoint access in LINQ translating to a server-side JSON path lookup. Opts out of the two-column default per property.</item>
/// </list>
/// Both shapes re-validate on read: a stored row violating <c>Start &lt;= End</c> throws when materialized — the JSON shape through its converter, the two-column shape through the validation that <c>UseStrongTypes()</c> registers.
/// </summary>
public static class IntervalEfCoreExtensions
{
    /// <summary>Maps the interval property to a single JSON column. The same converter handles all four interval types and re-validates the <c>Start &lt;= End</c> invariant on read.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TInterval">The interval struct type.</typeparam>
    /// <param name="entity">The entity-type builder.</param>
    /// <param name="propertyExpression">A lambda selecting the interval property.</param>
    public static PropertyBuilder<TInterval> HasIntervalJsonConversion<TEntity, TInterval>(
        this EntityTypeBuilder<TEntity> entity,
        Expression<Func<TEntity, TInterval>> propertyExpression)
        where TEntity : class
        where TInterval : struct =>
        entity.Property(propertyExpression).HasIntervalJsonConversion();

    /// <summary>Maps the interval property to a single JSON column. The same converter handles all four interval types and re-validates the <c>Start &lt;= End</c> invariant on read.</summary>
    /// <typeparam name="TInterval">The interval struct type.</typeparam>
    /// <param name="property">The property builder.</param>
    public static PropertyBuilder<TInterval> HasIntervalJsonConversion<TInterval>(this PropertyBuilder<TInterval> property)
        where TInterval : struct =>
        property.HasConversion(new IntervalJsonValueConverter<TInterval>());

    /// <summary>Maps the nullable interval property to a single JSON column; a <c>null</c> property maps to a <c>NULL</c> column. The same converter handles all four interval types and re-validates the <c>Start &lt;= End</c> invariant on read.</summary>
    /// <typeparam name="TInterval">The interval struct type.</typeparam>
    /// <param name="property">The property builder.</param>
    public static PropertyBuilder<TInterval?> HasIntervalJsonConversion<TInterval>(this PropertyBuilder<TInterval?> property)
        where TInterval : struct =>
        property.HasConversion(new IntervalJsonValueConverter<TInterval>());

    /// <summary>Maps the interval property to two scalar columns (<c>Start</c> and <c>End</c>) as an EF Core complex type, so each endpoint is its own queryable, indexable column. The endpoint columns are nullable exactly when the variant's endpoint is (<c>FiniteInterval</c> → both required; <c>IntervalFrom</c> → end nullable; and so on).</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TInterval">The interval struct type.</typeparam>
    /// <param name="entity">The entity-type builder.</param>
    /// <param name="propertyExpression">A lambda selecting the interval property.</param>
    /// <param name="startBound">How the start bound's inclusivity is persisted; <see cref="IntervalBoundMode.Stored"/> adds a <c>StartInclusive</c> column.</param>
    /// <param name="endBound">How the end bound's inclusivity is persisted; <see cref="IntervalBoundMode.Stored"/> adds an <c>EndInclusive</c> column.</param>
    public static ComplexPropertyBuilder<TInterval> HasIntervalColumns<TEntity, TInterval>(
        this EntityTypeBuilder<TEntity> entity,
        Expression<Func<TEntity, TInterval>> propertyExpression,
        IntervalBoundMode startBound = IntervalBoundMode.AlwaysInclusive,
        IntervalBoundMode endBound = IntervalBoundMode.AlwaysInclusive)
        where TEntity : class
        where TInterval : struct
    {
        var complexProperty = entity.ComplexProperty(propertyExpression);
        ConfigureBounds(complexProperty, startBound, endBound);
        return complexProperty;
    }

    /// <summary>Maps the nullable interval property to two scalar columns plus a shadow discriminator column that keeps a <c>null</c> property distinct from an interval whose stored endpoints are all <c>NULL</c> (a fully-unbounded <see cref="Interval{T}"/>).</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TInterval">The interval struct type.</typeparam>
    /// <param name="entity">The entity-type builder.</param>
    /// <param name="propertyExpression">A lambda selecting the interval property.</param>
    /// <param name="startBound">How the start bound's inclusivity is persisted; <see cref="IntervalBoundMode.Stored"/> adds a <c>StartInclusive</c> column.</param>
    /// <param name="endBound">How the end bound's inclusivity is persisted; <see cref="IntervalBoundMode.Stored"/> adds an <c>EndInclusive</c> column.</param>
    public static ComplexPropertyBuilder HasIntervalColumns<TEntity, TInterval>(
        this EntityTypeBuilder<TEntity> entity,
        Expression<Func<TEntity, TInterval?>> propertyExpression,
        IntervalBoundMode startBound = IntervalBoundMode.AlwaysInclusive,
        IntervalBoundMode endBound = IntervalBoundMode.AlwaysInclusive)
        where TEntity : class
        where TInterval : struct
    {
        var complexProperty = entity.ComplexProperty(typeof(TInterval?), PropertyName(propertyExpression));
        complexProperty.HasDiscriminator();
        ConfigureBounds(complexProperty, startBound, endBound);
        return complexProperty;
    }

    private static void ConfigureBounds(ComplexPropertyBuilder builder, IntervalBoundMode startBound, IntervalBoundMode endBound)
    {
        builder.Metadata.SetAnnotation(IntervalAnnotations.StartBound, startBound);
        builder.Metadata.SetAnnotation(IntervalAnnotations.EndBound, endBound);
        ConfigureBoundFlag(builder, startBound, "StartInclusive");
        ConfigureBoundFlag(builder, endBound, "EndInclusive");
    }

    private static void ConfigureBoundFlag(ComplexPropertyBuilder builder, IntervalBoundMode mode, string flagName)
    {
        if (mode == IntervalBoundMode.Stored)
        {
            builder.Property(flagName);
        }
        else
        {
            builder.Ignore(flagName);
        }
    }

    private static string PropertyName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        var body = propertyExpression.Body is UnaryExpression { Operand: MemberExpression converted } ? converted : (MemberExpression)propertyExpression.Body;
        return body.Member.Name;
    }
}

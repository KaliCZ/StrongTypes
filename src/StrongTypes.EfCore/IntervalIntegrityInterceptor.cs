using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace StrongTypes.EfCore;

/// <summary>Guards column-mapped interval properties. On materialization it re-validates the interval invariant (a stored row violating it throws instead of producing an invalid interval) and applies the configured <see cref="IntervalBoundMode"/> to bounds that have no flag column. On save it rejects a value whose bound contradicts an <c>Always*</c> mode. (JSON-mapped intervals re-validate in their converter and carry their bounds in the payload.)</summary>
internal sealed class IntervalIntegrityInterceptor : IMaterializationInterceptor, ISaveChangesInterceptor
{
    public static readonly IntervalIntegrityInterceptor Instance = new();

    private static readonly ConditionalWeakTable<IEntityType, Action<object>[]> s_readProcessors = new();
    private static readonly ConditionalWeakTable<IEntityType, Action<object>[]> s_saveCheckers = new();

    private IntervalIntegrityInterceptor()
    {
    }

    public object InitializedInstance(MaterializationInterceptionData materializationData, object instance)
    {
        foreach (var process in s_readProcessors.GetValue(materializationData.EntityType, BuildReadProcessors))
        {
            process(instance);
        }
        return instance;
    }

    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        EnforceBoundModes(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        EnforceBoundModes(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void EnforceBoundModes(DbContext? context)
    {
        if (context is null)
        {
            return;
        }
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }
            foreach (var check in s_saveCheckers.GetValue(entry.Metadata, BuildSaveCheckers))
            {
                check(entry.Entity);
            }
        }
    }

    private static Action<object>[] BuildReadProcessors(IEntityType entityType) =>
        IntervalComplexProperties(entityType)
            .Select(p => BuildReadProcessor(entityType.ClrType, p))
            .ToArray();

    private static Action<object>[] BuildSaveCheckers(IEntityType entityType) =>
        IntervalComplexProperties(entityType)
            .Select(p => BuildSaveChecker(entityType.ClrType, p))
            .OfType<Action<object>>()
            .ToArray();

    private static IEnumerable<IComplexProperty> IntervalComplexProperties(IEntityType entityType) =>
        entityType.GetComplexProperties().Where(p => IntervalTypes.IsInterval(p.ClrType) && p.PropertyInfo is not null);

    private static Action<object> BuildReadProcessor(Type entityClrType, IComplexProperty property)
    {
        var startMode = BoundMode(property, IntervalAnnotations.StartBound);
        var endMode = BoundMode(property, IntervalAnnotations.EndBound);
        if (startMode == IntervalBoundMode.AlwaysInclusive && endMode == IntervalBoundMode.AlwaysInclusive)
        {
            // Unmapped bounds materialize as inclusive, so the default mode only needs validation.
            return BuildValidator(entityClrType, property);
        }
        var propertyInfo = property.PropertyInfo!;
        var rebuild = FindShapeMethod(nameof(Rebuild), property.ClrType);
        var displayName = $"{entityClrType.Name}.{property.Name}";
        return instance =>
        {
            var value = propertyInfo.GetValue(instance);
            var corrected = rebuild.Invoke(
                null, BindingFlags.DoNotWrapExceptions, binder: null, [value, startMode, endMode, displayName], culture: null);
            if (!Equals(corrected, value))
            {
                propertyInfo.SetValue(instance, corrected);
            }
        };
    }

    private static Action<object>? BuildSaveChecker(Type entityClrType, IComplexProperty property)
    {
        var startMode = BoundMode(property, IntervalAnnotations.StartBound);
        var endMode = BoundMode(property, IntervalAnnotations.EndBound);
        if (startMode == IntervalBoundMode.Stored && endMode == IntervalBoundMode.Stored)
        {
            return null;
        }
        var propertyInfo = property.PropertyInfo!;
        var check = FindShapeMethod(nameof(CheckBounds), property.ClrType);
        var displayName = $"{entityClrType.Name}.{property.Name}";
        return instance => check.Invoke(
            null, BindingFlags.DoNotWrapExceptions, binder: null, [propertyInfo.GetValue(instance), startMode, endMode, displayName], culture: null);
    }

    private static IntervalBoundMode BoundMode(IComplexProperty property, string annotation) =>
        property.FindAnnotation(annotation)?.Value is IntervalBoundMode mode ? mode : IntervalBoundMode.AlwaysInclusive;

    private static Action<object> BuildValidator(Type entityClrType, IComplexProperty property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Property(Expression.Convert(instance, entityClrType), property.PropertyInfo!);
        var body = Expression.Call(
            FindShapeMethod(nameof(Validate), property.ClrType), value, Expression.Constant($"{entityClrType.Name}.{property.Name}"));
        return Expression.Lambda<Action<object>>(body, instance).Compile();
    }

    private static MethodInfo FindShapeMethod(string name, Type clrType)
    {
        var unwrapped = Nullable.GetUnderlyingType(clrType) ?? clrType;
        var endpoint = unwrapped.GetGenericArguments()[0];
        return typeof(IntervalIntegrityInterceptor)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == name)
            .Select(m => m.MakeGenericMethod(endpoint))
            .Single(m => m.GetParameters()[0].ParameterType == clrType);
    }

    private static void Validate<T>(FiniteInterval<T> interval, string property) where T : struct, IComparable<T>
    {
        if (interval.Start.CompareTo(interval.End) > 0) Throw(interval, property);
    }

    private static void Validate<T>(Interval<T> interval, string property) where T : struct, IComparable<T>
    {
        if (interval.Start is { } start && interval.End is { } end && start.CompareTo(end) > 0) Throw(interval, property);
    }

    private static void Validate<T>(IntervalFrom<T> interval, string property) where T : struct, IComparable<T>
    {
        if (interval.End is { } end && interval.Start.CompareTo(end) > 0) Throw(interval, property);
    }

    private static void Validate<T>(IntervalUntil<T> interval, string property) where T : struct, IComparable<T>
    {
        if (interval.Start is { } start && start.CompareTo(interval.End) > 0) Throw(interval, property);
    }

    private static void Validate<T>(FiniteInterval<T>? interval, string property) where T : struct, IComparable<T>
    {
        if (interval is { } value) Validate(value, property);
    }

    private static void Validate<T>(Interval<T>? interval, string property) where T : struct, IComparable<T>
    {
        if (interval is { } value) Validate(value, property);
    }

    private static void Validate<T>(IntervalFrom<T>? interval, string property) where T : struct, IComparable<T>
    {
        if (interval is { } value) Validate(value, property);
    }

    private static void Validate<T>(IntervalUntil<T>? interval, string property) where T : struct, IComparable<T>
    {
        if (interval is { } value) Validate(value, property);
    }

    private static void Throw(object interval, string property) =>
        throw new InvalidOperationException($"Interval read from the database for '{property}' violates Start <= End: {interval}.");

    private static FiniteInterval<T> Rebuild<T>(
        FiniteInterval<T> interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T> =>
        FiniteInterval<T>.TryCreate(
            interval.Start, interval.End, ResolveBound(startMode, interval.StartInclusive), ResolveBound(endMode, interval.EndInclusive))
            ?? throw ReadInvalid(property, interval.Start, interval.End);

    private static Interval<T> Rebuild<T>(
        Interval<T> interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T> =>
        Interval<T>.TryCreate(
            interval.Start, interval.End, ResolveBound(startMode, interval.StartInclusive), ResolveBound(endMode, interval.EndInclusive))
            ?? throw ReadInvalid(property, interval.Start, interval.End);

    private static IntervalFrom<T> Rebuild<T>(
        IntervalFrom<T> interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T> =>
        IntervalFrom<T>.TryCreate(
            interval.Start, interval.End, ResolveBound(startMode, interval.StartInclusive), ResolveBound(endMode, interval.EndInclusive))
            ?? throw ReadInvalid(property, interval.Start, interval.End);

    private static IntervalUntil<T> Rebuild<T>(
        IntervalUntil<T> interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T> =>
        IntervalUntil<T>.TryCreate(
            interval.Start, interval.End, ResolveBound(startMode, interval.StartInclusive), ResolveBound(endMode, interval.EndInclusive))
            ?? throw ReadInvalid(property, interval.Start, interval.End);

    private static FiniteInterval<T>? Rebuild<T>(
        FiniteInterval<T>? interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T> =>
        interval is { } value ? Rebuild(value, startMode, endMode, property) : null;

    private static Interval<T>? Rebuild<T>(
        Interval<T>? interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T> =>
        interval is { } value ? Rebuild(value, startMode, endMode, property) : null;

    private static IntervalFrom<T>? Rebuild<T>(
        IntervalFrom<T>? interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T> =>
        interval is { } value ? Rebuild(value, startMode, endMode, property) : null;

    private static IntervalUntil<T>? Rebuild<T>(
        IntervalUntil<T>? interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T> =>
        interval is { } value ? Rebuild(value, startMode, endMode, property) : null;

    private static bool ResolveBound(IntervalBoundMode mode, bool stored) => mode switch
    {
        IntervalBoundMode.AlwaysInclusive => true,
        IntervalBoundMode.AlwaysExclusive => false,
        _ => stored,
    };

    private static InvalidOperationException ReadInvalid(string property, object? start, object? end) =>
        new($"Interval read from the database for '{property}' is not valid for its configured bounds: start {start}, end {end}.");

    private static void CheckBounds<T>(
        FiniteInterval<T> interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T>
    {
        CheckBound(interval.StartInclusive, startMode, property, "start");
        CheckBound(interval.EndInclusive, endMode, property, "end");
    }

    private static void CheckBounds<T>(
        Interval<T> interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T>
    {
        if (interval.Start.HasValue) CheckBound(interval.StartInclusive, startMode, property, "start");
        if (interval.End.HasValue) CheckBound(interval.EndInclusive, endMode, property, "end");
    }

    private static void CheckBounds<T>(
        IntervalFrom<T> interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T>
    {
        CheckBound(interval.StartInclusive, startMode, property, "start");
        if (interval.End.HasValue) CheckBound(interval.EndInclusive, endMode, property, "end");
    }

    private static void CheckBounds<T>(
        IntervalUntil<T> interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T>
    {
        if (interval.Start.HasValue) CheckBound(interval.StartInclusive, startMode, property, "start");
        CheckBound(interval.EndInclusive, endMode, property, "end");
    }

    private static void CheckBounds<T>(
        FiniteInterval<T>? interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T>
    {
        if (interval is { } value) CheckBounds(value, startMode, endMode, property);
    }

    private static void CheckBounds<T>(
        Interval<T>? interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T>
    {
        if (interval is { } value) CheckBounds(value, startMode, endMode, property);
    }

    private static void CheckBounds<T>(
        IntervalFrom<T>? interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T>
    {
        if (interval is { } value) CheckBounds(value, startMode, endMode, property);
    }

    private static void CheckBounds<T>(
        IntervalUntil<T>? interval, IntervalBoundMode startMode, IntervalBoundMode endMode, string property)
        where T : struct, IComparable<T>
    {
        if (interval is { } value) CheckBounds(value, startMode, endMode, property);
    }

    private static void CheckBound(bool inclusive, IntervalBoundMode mode, string property, string endpoint)
    {
        var expected = mode switch
        {
            IntervalBoundMode.AlwaysInclusive => true,
            IntervalBoundMode.AlwaysExclusive => false,
            _ => inclusive,
        };
        if (inclusive != expected)
        {
            throw new InvalidOperationException(
                $"Cannot save '{property}': the {endpoint} bound is {(inclusive ? "inclusive" : "exclusive")} but the column mapping is "
                + $"{mode}. Map the property with HasIntervalColumns(..., {endpoint}Bound: IntervalBoundMode.Stored) to store per-value bounds.");
        }
    }
}

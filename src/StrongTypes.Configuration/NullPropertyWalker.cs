using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace StrongTypes.Configuration;

/// <summary>Every property a declaration says can never be null but that binding left null, at any depth.</summary>
internal sealed class NullPropertyWalker
{
    private static readonly ConcurrentDictionary<Type, bool> LeafCache = new();

    private readonly NullabilityInfoContext nullability = new();
    private readonly HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
    private readonly List<string> failures = [];

    private NullPropertyWalker() { }

    public static IReadOnlyList<string> Collect(object root, string rootPath)
    {
        var walker = new NullPropertyWalker();
        walker.WalkObject(root, rootPath);
        return walker.failures;
    }

    private void WalkObject(object instance, string path)
    {
        // Two properties can hold the same instance, and a graph can cycle; without this the first
        // duplicates every failure beneath it and the second never terminates.
        if (!visited.Add(instance))
        {
            return;
        }

        foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetGetMethod() is null)
            {
                continue;
            }

            var value = property.GetValue(instance);
            if (value is null)
            {
                if (LeftNullAgainstItsDeclaration(property))
                {
                    failures.Add(Describe(property, Join(path, property.Name)));
                }
                continue;
            }

            WalkValue(value, Join(path, property.Name));
        }
    }

    private void WalkValue(object value, string path)
    {
        if (IsLeaf(value.GetType()))
        {
            return;
        }

        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value is not null)
                {
                    WalkValue(entry.Value, Join(path, entry.Key.ToString() ?? ""));
                }
            }
            return;
        }

        if (value is IEnumerable items)
        {
            var index = 0;
            foreach (var item in items)
            {
                if (item is not null)
                {
                    WalkValue(item, Join(path, index.ToString()));
                }
                index++;
            }
            return;
        }

        WalkObject(value, path);
    }

    /// <summary>
    /// A value type is skipped because it has no invalid state to reach: an unconfigured
    /// <c>Positive&lt;int&gt;</c> is <c>1</c> and an unconfigured <c>bool</c> is <c>false</c> — values
    /// those types are happy to hold. A property with no setter is skipped because the binder never
    /// assigns one, so its being null is not something configuration could have fixed.
    /// </summary>
    private bool LeftNullAgainstItsDeclaration(PropertyInfo property) =>
        !property.PropertyType.IsValueType
        && property.GetSetMethod() is not null
        && nullability.Create(property).WriteState == NullabilityState.NotNull;

    /// <summary>
    /// Stop where <c>ConfigurationBinder</c> stops: it converts a type it has a string converter for
    /// and recurses into everything else, so this walks exactly the graph the binder built.
    /// </summary>
    private static bool IsLeaf(Type type) =>
        LeafCache.GetOrAdd(type, static t => TypeDescriptor.GetConverter(t).CanConvertFrom(typeof(string)));

    private static string Join(string path, string name) => path.Length == 0 ? name : $"{path}:{name}";

    private static string Describe(PropertyInfo property, string path) =>
        $"'{path}' is null. Configure it, give {property.DeclaringType!.Name}.{property.Name} a default, or declare it nullable.";
}

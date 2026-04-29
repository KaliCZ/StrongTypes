using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrongTypes.OpenApi.Core;

/// <summary>
/// Computes the OpenAPI <c>required</c> set for a CLR record / class —
/// non-nullable properties are required, nullable properties are not.
/// The two ASP.NET Core OpenAPI pipelines disagree on the default
/// (Microsoft.AspNetCore.OpenApi puts every property in <c>required</c>
/// regardless of nullability; Swashbuckle puts none); calling this from
/// each pipeline's property-level pass aligns both with the standard
/// "non-nullable C# → required" semantic.
/// </summary>
public static class RequiredSet
{
    public static HashSet<string> ComputeJsonNames(Type type)
    {
        var nullability = new NullabilityInfoContext();
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (IsNullable(p, nullability)) continue;
            result.Add(ResolveJsonName(p));
        }
        return result;
    }

    private static bool IsNullable(PropertyInfo property, NullabilityInfoContext nullability)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null) return true;
        if (property.PropertyType.IsValueType) return false;

        var info = nullability.Create(property);
        return info.ReadState == NullabilityState.Nullable
            || info.WriteState == NullabilityState.Nullable;
    }

    private static string ResolveJsonName(PropertyInfo property)
    {
        var jsonNameAttr = property.GetCustomAttributes(inherit: true)
            .OfType<JsonPropertyNameAttribute>()
            .FirstOrDefault();
        return jsonNameAttr is not null
            ? jsonNameAttr.Name
            : JsonNamingPolicy.CamelCase.ConvertName(property.Name);
    }
}

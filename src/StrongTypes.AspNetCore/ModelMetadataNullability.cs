using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StrongTypes.AspNetCore;

internal static class ModelMetadataNullability
{
    private static readonly NullabilityInfoContext s_nullability = new();

    public static bool IsNullable(ModelMetadata metadata)
    {
        if (metadata.ModelType.IsGenericType && metadata.ModelType.GetGenericTypeDefinition() == typeof(Nullable<>))
            return true;

        if (metadata.ContainerType is null || metadata.PropertyName is null)
            return metadata.IsReferenceOrNullableType;

        var property = metadata.ContainerType.GetProperty(
            metadata.PropertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        return property is not null
            ? s_nullability.Create(property).ReadState == NullabilityState.Nullable
            : metadata.IsReferenceOrNullableType;
    }
}

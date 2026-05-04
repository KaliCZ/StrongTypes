using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StrongTypes.AspNetCore;

/// <summary>Resolves an <see cref="IModelBinder"/> for action parameters typed as <see cref="NonEmptyEnumerable{T}"/>.</summary>
/// <remarks>The binder reads raw values from the request source (header / route / query / form), parses each via the element type's <see cref="System.ComponentModel.TypeConverter"/>, and wraps the result via <see cref="NonEmptyEnumerable.TryCreateRange{T}(System.Collections.Generic.IEnumerable{T})"/>; an empty source surfaces as a binding error.</remarks>
public sealed class NonEmptyEnumerableModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;
        if (!modelType.IsGenericType || modelType.GetGenericTypeDefinition() != typeof(NonEmptyEnumerable<>))
            return null;

        var elementType = modelType.GetGenericArguments()[0];
        var binderType = typeof(NonEmptyEnumerableModelBinder<>).MakeGenericType(elementType);
        return (IModelBinder)Activator.CreateInstance(binderType, ModelMetadataNullability.IsNullable(context.Metadata))!;
    }
}

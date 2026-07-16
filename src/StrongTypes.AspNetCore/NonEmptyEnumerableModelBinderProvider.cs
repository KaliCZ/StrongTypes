using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StrongTypes.AspNetCore;

/// <summary>Resolves an <see cref="IModelBinder"/> for action parameters typed as <see cref="NonEmptyEnumerable{T}"/>.</summary>
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

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StrongTypes.AspNetCore;

/// <summary>Resolves an <see cref="IModelBinder"/> for action parameters typed as <see cref="Maybe{T}"/>.</summary>
/// <remarks>Useful for form-bound patch contracts where nullable <c>Maybe&lt;T&gt;</c> can preserve omitted vs empty vs populated fields.</remarks>
public sealed class MaybeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;
        var isNullable = ModelMetadataNullability.IsNullable(context.Metadata);
        if (modelType.IsGenericType && modelType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            modelType = modelType.GetGenericArguments()[0];
            isNullable = true;
        }

        if (!modelType.IsGenericType || modelType.GetGenericTypeDefinition() != typeof(Maybe<>))
            return null;

        var innerType = modelType.GetGenericArguments()[0];
        var binderType = typeof(MaybeModelBinder<>).MakeGenericType(innerType);
        return (IModelBinder)Activator.CreateInstance(binderType, isNullable)!;
    }
}

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StrongTypes.AspNetCore;

/// <summary>Resolves an <see cref="IModelBinder"/> for action parameters typed as <see cref="Maybe{T}"/>.</summary>
/// <remarks>An absent value provider entry binds to <c>None</c>; a present entry delegates to the framework's binder for the inner type and wraps the result in <c>Some</c>.</remarks>
public sealed class MaybeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;
        if (!modelType.IsGenericType || modelType.GetGenericTypeDefinition() != typeof(Maybe<>))
            return null;

        var innerType = modelType.GetGenericArguments()[0];
        var binderType = typeof(MaybeModelBinder<>).MakeGenericType(innerType);
        return (IModelBinder)Activator.CreateInstance(binderType)!;
    }
}

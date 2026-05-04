using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StrongTypes.AspNetCore;

/// <summary>Binds <see cref="Maybe{T}"/> from a non-body source. For nullable Maybe targets, an absent source binds to <c>null</c>, an empty source binds to <c>None</c>, and a non-empty source parses via <see cref="IParsable{TSelf}"/> on <typeparamref name="T"/> and wraps it as <c>Some</c>.</summary>
/// <typeparam name="T">The wrapped type. Must implement <see cref="IParsable{TSelf}"/> (every BCL primitive in net7+ and every Kalicz.StrongTypes wrapper qualifies).</typeparam>
public sealed class MaybeModelBinder<T> : IModelBinder
    where T : notnull
{
    private readonly bool _isNullable;

    public MaybeModelBinder()
    {
    }

    public MaybeModelBinder(bool isNullable)
    {
        _isNullable = isNullable;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);
        var modelName = bindingContext.ModelName;

        if (!StringElementParser<T>.IsSupported)
        {
            bindingContext.ModelState.TryAddModelError(modelName, $"Maybe<{typeof(T).Name}> can't bind: {typeof(T).Name} doesn't implement IParsable<{typeof(T).Name}>.");
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var (raw, present) = ReadRawValue(bindingContext);
        if (!present)
        {
            var isNullable = _isNullable || ModelMetadataNullability.IsNullable(bindingContext.ModelMetadata);
            bindingContext.Result = ModelBindingResult.Success(isNullable ? null : default(Maybe<T>));
            return Task.CompletedTask;
        }

        if (string.IsNullOrEmpty(raw))
        {
            bindingContext.Result = ModelBindingResult.Success(default(Maybe<T>));
            return Task.CompletedTask;
        }

        if (!StringElementParser<T>.TryParse(raw, out var value))
        {
            bindingContext.ModelState.TryAddModelError(modelName, $"Could not parse '{raw}' as {typeof(T).Name}.");
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(Maybe<T>.Some(value));
        return Task.CompletedTask;
    }

    private static (string? raw, bool present) ReadRawValue(ModelBindingContext bindingContext)
    {
        var bindingSource = bindingContext.BindingSource;
        if (bindingSource is not null && bindingSource.CanAcceptDataFrom(BindingSource.Header))
        {
            var headers = bindingContext.HttpContext.Request.Headers;
            if (!headers.TryGetValue(bindingContext.FieldName, out var values))
                return (null, false);
            return (values.ToString(), true);
        }

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return (null, false);

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        return (valueProviderResult.FirstValue, true);
    }
}

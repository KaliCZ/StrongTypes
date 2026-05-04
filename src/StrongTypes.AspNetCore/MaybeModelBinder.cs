using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StrongTypes.AspNetCore;

/// <summary>Binds an action parameter of type <see cref="Maybe{T}"/> from any non-body source. An absent source binds to <c>None</c>; a present source parses the raw value via the registered <see cref="TypeConverter"/> for <typeparamref name="T"/> and wraps it as <c>Some</c>; a parse failure surfaces as a <c>400</c>.</summary>
/// <typeparam name="T">The wrapped type.</typeparam>
public sealed class MaybeModelBinder<T> : IModelBinder
    where T : notnull
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);
        var modelName = bindingContext.ModelName;

        var (raw, present) = ReadRawValue(bindingContext);
        if (!present || string.IsNullOrEmpty(raw))
        {
            bindingContext.Result = ModelBindingResult.Success(default(Maybe<T>));
            return Task.CompletedTask;
        }

        var converter = TypeDescriptor.GetConverter(typeof(T));
        try
        {
            var value = (T)converter.ConvertFromString(context: null, CultureInfo.InvariantCulture, raw)!;
            bindingContext.Result = ModelBindingResult.Success(Maybe<T>.Some(value));
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.TryAddModelError(modelName, $"Could not parse '{raw}' as {typeof(T).Name}: {ex.Message}");
            bindingContext.Result = ModelBindingResult.Failed();
        }
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

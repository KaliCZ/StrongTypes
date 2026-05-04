using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace StrongTypes.AspNetCore;

/// <summary>Binds an action parameter of type <see cref="NonEmptyEnumerable{T}"/> from any non-body source. Reads multiple raw strings from the binding source, parses each via <see cref="IParsable{TSelf}"/> on <typeparamref name="T"/>, and wraps the result via <see cref="NonEmptyEnumerable.TryCreateRange{T}(System.Collections.Generic.IEnumerable{T})"/>; an empty source surfaces as a binding error.</summary>
/// <typeparam name="T">The element type. Must implement <see cref="IParsable{TSelf}"/> (every BCL primitive in net7+ and every Kalicz.StrongTypes wrapper qualifies).</typeparam>
public sealed class NonEmptyEnumerableModelBinder<T> : IModelBinder
{
    private readonly bool _isNullable;

    public NonEmptyEnumerableModelBinder()
    {
    }

    public NonEmptyEnumerableModelBinder(bool isNullable)
    {
        _isNullable = isNullable;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);
        var modelName = bindingContext.ModelName;

        if (!StringElementParser<T>.IsSupported)
        {
            bindingContext.ModelState.TryAddModelError(modelName, $"NonEmptyEnumerable<{typeof(T).Name}> can't bind: {typeof(T).Name} doesn't implement IParsable<{typeof(T).Name}>.");
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var raw = ReadRawValues(bindingContext);
        if (raw.Count == 0)
        {
            if (_isNullable || ModelMetadataNullability.IsNullable(bindingContext.ModelMetadata))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.TryAddModelError(modelName, $"At least one value is required for '{modelName}'.");
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var array = new T[raw.Count];
        for (var i = 0; i < raw.Count; i++)
        {
            if (!StringElementParser<T>.TryParse(raw[i] ?? string.Empty, out array[i]!))
            {
                bindingContext.ModelState.TryAddModelError(modelName, $"Could not parse '{raw[i]}' as {typeof(T).Name}.");
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }
        }

        var nonEmpty = NonEmptyEnumerable.TryCreateRange(array)!;
        bindingContext.Result = ModelBindingResult.Success(nonEmpty);
        return Task.CompletedTask;
    }

    private static StringValues ReadRawValues(ModelBindingContext bindingContext)
    {
        var bindingSource = bindingContext.BindingSource;
        if (bindingSource is not null && bindingSource.CanAcceptDataFrom(BindingSource.Header))
        {
            // Headers don't flow through the value provider chain — only HeaderModelBinder
            // sees them. Read them ourselves so the FieldName lookup matches what
            // [FromHeader(Name = "...")] specified. Multiple values arrive either as
            // repeated header lines (StringValues with multiple entries) or as a
            // comma-separated single line — accept both.
            var headers = bindingContext.HttpContext.Request.Headers;
            if (!headers.TryGetValue(bindingContext.FieldName, out var values))
                return StringValues.Empty;

            var split = new List<string>();
            foreach (var line in values)
            {
                if (line is null) continue;
                foreach (var piece in line.Split(','))
                {
                    var trimmed = piece.Trim();
                    if (trimmed.Length > 0) split.Add(trimmed);
                }
            }
            return new StringValues(split.ToArray());
        }

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return StringValues.Empty;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        return valueProviderResult.Values;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace StrongTypes.AspNetCore;

/// <summary>Binds an action parameter of type <see cref="NonEmptyEnumerable{T}"/> from any non-body source. Reads multiple raw values from the binding source, parses each via the registered <see cref="TypeConverter"/> for <typeparamref name="T"/>, and wraps the result via <see cref="NonEmptyEnumerable.TryCreateRange{T}(System.Collections.Generic.IEnumerable{T})"/>; an empty source surfaces as a binding error.</summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class NonEmptyEnumerableModelBinder<T> : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);
        var modelName = bindingContext.ModelName;

        var raw = ReadRawValues(bindingContext);
        if (raw.Count == 0)
        {
            bindingContext.ModelState.TryAddModelError(modelName, $"At least one value is required for '{modelName}'.");
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var converter = TypeDescriptor.GetConverter(typeof(T));
        var array = new T[raw.Count];
        for (var i = 0; i < raw.Count; i++)
        {
            try
            {
                array[i] = (T)converter.ConvertFromString(context: null, CultureInfo.InvariantCulture, raw[i]!)!;
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(modelName, $"Could not parse '{raw[i]}' as {typeof(T).Name}: {ex.Message}");
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

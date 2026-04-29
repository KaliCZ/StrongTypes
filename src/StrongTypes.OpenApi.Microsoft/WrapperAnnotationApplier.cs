using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

// Source: Microsoft.AspNetCore.OpenApi 10.0 — JsonNodeSchemaExtensions.ApplyValidationAttributes
// and OpenApiSchemaService's per-property attribute pass. Keep this in step on runtime upgrades.
internal static class WrapperAnnotationApplier
{
    public static bool TryApply(OpenApiSchema schema, Type? propertyClrType, IEnumerable<Attribute> attributes)
    {
        if (propertyClrType is null) return false;

        var unwrapped = Nullable.GetUnderlyingType(propertyClrType) ?? propertyClrType;
        var attrList = attributes as IReadOnlyList<Attribute> ?? attributes.ToArray();

        var matched = false;
        if (unwrapped == typeof(NonEmptyString))
        {
            ApplyStringAnnotations(schema, attrList);
            matched = true;
        }
        else if (unwrapped.IsGenericType)
        {
            var def = unwrapped.GetGenericTypeDefinition();
            if (def == typeof(Positive<>) || def == typeof(NonNegative<>) ||
                def == typeof(Negative<>) || def == typeof(NonPositive<>))
            {
                ApplyNumericAnnotations(schema, attrList);
                matched = true;
            }
            else if (def == typeof(NonEmptyEnumerable<>) || def == typeof(INonEmptyEnumerable<>))
            {
                ApplyArrayAnnotations(schema, attrList);
                matched = true;
            }
        }

        if (!matched) return false;

        ApplyUniversalAnnotations(schema, attrList);
        return true;
    }

    private static void ApplyStringAnnotations(OpenApiSchema schema, IReadOnlyList<Attribute> attrs)
    {
        foreach (var a in attrs)
        {
            switch (a)
            {
                case StringLengthAttribute sl:
                    if (sl.MinimumLength > 0) SchemaPaint.TightenMinLength(schema, sl.MinimumLength);
                    if (sl.MaximumLength > 0) SchemaPaint.TightenMaxLength(schema, sl.MaximumLength);
                    break;
                case LengthAttribute len:
                    SchemaPaint.TightenMinLength(schema, len.MinimumLength);
                    SchemaPaint.TightenMaxLength(schema, len.MaximumLength);
                    break;
                case MinLengthAttribute ml:
                    SchemaPaint.TightenMinLength(schema, ml.Length);
                    break;
                case MaxLengthAttribute mxl:
                    SchemaPaint.TightenMaxLength(schema, mxl.Length);
                    break;
                case RegularExpressionAttribute re:
                    SchemaPaint.SetPatternIfAbsent(schema, re.Pattern);
                    break;
                case EmailAddressAttribute:
                    SchemaPaint.SetFormatIfAbsent(schema, "email");
                    break;
                case UrlAttribute:
                    SchemaPaint.SetFormatIfAbsent(schema, "uri");
                    break;
                case Base64StringAttribute:
                    SchemaPaint.SetFormatIfAbsent(schema, "byte");
                    break;
            }
        }
    }

    private static void ApplyNumericAnnotations(OpenApiSchema schema, IReadOnlyList<Attribute> attrs)
    {
        foreach (var a in attrs)
        {
            if (a is RangeAttribute range)
            {
                if (TryToDecimal(range.Minimum, out var min))
                    SchemaPaint.TightenLowerBound(schema, min, floorExclusive: range.MinimumIsExclusive);
                if (TryToDecimal(range.Maximum, out var max))
                    SchemaPaint.TightenUpperBound(schema, max, floorExclusive: range.MaximumIsExclusive);
            }
        }
    }

    private static void ApplyArrayAnnotations(OpenApiSchema schema, IReadOnlyList<Attribute> attrs)
    {
        foreach (var a in attrs)
        {
            switch (a)
            {
                case LengthAttribute len:
                    SchemaPaint.TightenMinItems(schema, len.MinimumLength);
                    SchemaPaint.TightenMaxItems(schema, len.MaximumLength);
                    break;
                case MinLengthAttribute ml:
                    SchemaPaint.TightenMinItems(schema, ml.Length);
                    break;
                case MaxLengthAttribute mxl:
                    SchemaPaint.TightenMaxItems(schema, mxl.Length);
                    break;
            }
        }
    }

    private static void ApplyUniversalAnnotations(OpenApiSchema schema, IReadOnlyList<Attribute> attrs)
    {
        foreach (var a in attrs)
        {
            if (a is DescriptionAttribute desc && !string.IsNullOrEmpty(desc.Description))
                SchemaPaint.SetDescriptionIfAbsent(schema, desc.Description);
        }
    }

    private static bool TryToDecimal(object? value, out decimal result)
    {
        result = 0m;
        if (value is null) return false;
        try
        {
            result = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}

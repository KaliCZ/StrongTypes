using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Core;

/// <summary>
/// Layers caller-supplied data-annotations
/// (<see cref="StringLengthAttribute"/>, <see cref="RangeAttribute"/>,
/// <see cref="RegularExpressionAttribute"/>, <see cref="DescriptionAttribute"/>, …)
/// onto a schema painted for a strong-type wrapper. Bounds are tightened
/// via the <see cref="SchemaPaint"/> helpers so caller-stricter values
/// stack on top of the wrapper's own floor/ceiling without weakening
/// either side. Returns <c>true</c> when the wrapper type was recognised
/// and the attribute pass ran (regardless of whether any individual
/// attribute matched).
/// </summary>
public static class WrapperAnnotationApplier
{
    public static bool TryApply(OpenApiSchema schema, Type? propertyClrType, IEnumerable<Attribute> attributes)
    {
        if (propertyClrType is null) return false;

        var attrList = attributes as IReadOnlyList<Attribute> ?? attributes.ToArray();

        var matched = false;
        if (StrongTypeSchemaTypes.IsNonEmptyString(propertyClrType) || StrongTypeSchemaTypes.IsEmail(propertyClrType))
        {
            ApplyStringAnnotations(schema, attrList);
            matched = true;
        }
        else if (StrongTypeSchemaTypes.IsDigit(propertyClrType))
        {
            ApplyNumericAnnotations(schema, attrList);
            matched = true;
        }
        else if (StrongTypeSchemaTypes.TryGetNumeric(propertyClrType, out _, out _))
        {
            ApplyNumericAnnotations(schema, attrList);
            matched = true;
        }
        else if (StrongTypeSchemaTypes.TryGetNonEmptyEnumerableElement(propertyClrType, out _))
        {
            ApplyArrayAnnotations(schema, attrList);
            matched = true;
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

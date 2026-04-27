using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Core;

/// <summary>
/// Applies caller-supplied data-annotations to the on-the-wire schema for a
/// property whose CLR type is a strong-type wrapper. The two ASP.NET Core
/// OpenAPI pipelines (Microsoft.AspNetCore.OpenApi and Swashbuckle) both
/// strip <c>[StringLength]</c>, <c>[RegularExpression]</c>, <c>[Range]</c>
/// and the like before our type-level transformers / filters get to see
/// them; this helper, called from each pipeline's property-level pass,
/// reads the annotations off the property and layers them back on top of
/// the wrapper's wire shape using the <see cref="SchemaPaint"/> tighten-
/// wins rules.
/// </summary>
public static class WrapperAnnotationApplier
{
    public static bool TryApply(OpenApiSchema schema, Type? propertyClrType, IEnumerable<Attribute> attributes)
    {
        if (propertyClrType is null) return false;

        var unwrapped = Nullable.GetUnderlyingType(propertyClrType) ?? propertyClrType;
        var attrList = attributes as IReadOnlyList<Attribute> ?? attributes.ToArray();

        if (unwrapped == typeof(NonEmptyString))
        {
            ApplyStringAnnotations(schema, attrList);
            return true;
        }

        if (!unwrapped.IsGenericType) return false;
        var def = unwrapped.GetGenericTypeDefinition();

        if (def == typeof(Positive<>) || def == typeof(NonNegative<>) ||
            def == typeof(Negative<>) || def == typeof(NonPositive<>))
        {
            ApplyNumericAnnotations(schema, attrList);
            return true;
        }

        if (def == typeof(NonEmptyEnumerable<>) || def == typeof(INonEmptyEnumerable<>))
        {
            ApplyArrayAnnotations(schema, attrList);
            return true;
        }

        return false;
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
                case MinLengthAttribute ml:
                    SchemaPaint.TightenMinLength(schema, ml.Length);
                    break;
                case MaxLengthAttribute mxl:
                    SchemaPaint.TightenMaxLength(schema, mxl.Length);
                    break;
                case RegularExpressionAttribute re:
                    SchemaPaint.SetPatternIfAbsent(schema, re.Pattern);
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
                    SchemaPaint.TightenLowerBound(schema, min, floorExclusive: false);
                if (TryToDecimal(range.Maximum, out var max))
                    SchemaPaint.TightenUpperBound(schema, max, floorExclusive: false);
            }
        }
    }

    private static void ApplyArrayAnnotations(OpenApiSchema schema, IReadOnlyList<Attribute> attrs)
    {
        foreach (var a in attrs)
        {
            switch (a)
            {
                case MinLengthAttribute ml:
                    SchemaPaint.TightenMinItems(schema, ml.Length);
                    break;
                case MaxLengthAttribute mxl:
                    SchemaPaint.TightenMaxItems(schema, mxl.Length);
                    break;
            }
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

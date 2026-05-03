using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;

namespace StrongTypes.OpenApi.Microsoft;

// Microsoft.AspNetCore.OpenApi's default schema-id strategy: primitives are
// named by their C# keyword ("int", "long", "decimal", …) and generic
// wrappers compose with an "Of" infix. We layer a "StrongTypes" prefix on
// top of the default for our own wrapper types — see Startup.AddStrongTypes
// — so `Positive<int>` produces "StrongTypesPositiveOfint" rather than the
// ambiguous "PositiveOfint". The prefix shrinks the chance that a user DTO
// accidentally collides with a wrapper component name. Both the document-
// time component filler and the property-annotation transformer parse or
// rebuild these names; they all read the prefix from here.
internal static class MicrosoftSchemaNaming
{
    public const string WrapperPrefix = "StrongTypes";

    // Un-prefixed forms. The component filler strips the optional
    // "StrongTypes" prefix before calling TryMatchNumericComponent, so
    // these patterns recognise both the prefixed and un-prefixed names
    // the framework emits.
    private static readonly Dictionary<Type, string> s_numericPrefixByDefinition = new()
    {
        [typeof(Positive<>)]    = "PositiveOf",
        [typeof(NonNegative<>)] = "NonNegativeOf",
        [typeof(Negative<>)]    = "NegativeOf",
        [typeof(NonPositive<>)] = "NonPositiveOf",
    };

    private static readonly HashSet<Type> s_genericWrapperDefinitions =
    [
        typeof(Positive<>),
        typeof(NonNegative<>),
        typeof(Negative<>),
        typeof(NonPositive<>),
        typeof(NonEmptyEnumerable<>),
        typeof(INonEmptyEnumerable<>),
        typeof(Maybe<>),
    ];

    public static bool IsStrongTypeWrapper(Type type)
    {
        if (type == typeof(NonEmptyString)) return true;
        if (type == typeof(Email)) return true;
        if (!type.IsGenericType) return false;
        return s_genericWrapperDefinitions.Contains(type.GetGenericTypeDefinition());
    }

    private static readonly (string Prefix, NumericBound Bound)[] s_numericPrefixesWithBounds =
        NumericWrapperKinds.All
            .Select(k => (Prefix: s_numericPrefixByDefinition[k.GenericDefinition], k.Bound))
            .ToArray();

    private static readonly (Type ClrType, string Keyword)[] s_primitiveKeywords =
    [
        (typeof(sbyte),   "sbyte"),
        (typeof(byte),    "byte"),
        (typeof(short),   "short"),
        (typeof(ushort),  "ushort"),
        (typeof(int),     "int"),
        (typeof(uint),    "uint"),
        (typeof(long),    "long"),
        (typeof(ulong),   "ulong"),
        (typeof(float),   "float"),
        (typeof(double),  "double"),
        (typeof(decimal), "decimal"),
        (typeof(string),  "string"),
        (typeof(char),    "char"),
        (typeof(bool),    "bool"),
    ];

    private static readonly Dictionary<Type, string> s_typeToKeyword =
        s_primitiveKeywords.ToDictionary(e => e.ClrType, e => e.Keyword);

    private static readonly Dictionary<string, Type> s_keywordToType =
        s_primitiveKeywords.ToDictionary(e => e.Keyword, e => e.ClrType, StringComparer.Ordinal);

    public static bool TryMatchNumericComponent(string name, out string innerKeyword, out NumericBound bound)
    {
        foreach (var (prefix, b) in s_numericPrefixesWithBounds)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                innerKeyword = name[prefix.Length..];
                bound = b;
                return true;
            }
        }

        innerKeyword = string.Empty;
        bound = default;
        return false;
    }

    public static string? GetPrimitiveKeyword(Type clrType) =>
        s_typeToKeyword.TryGetValue(clrType, out var keyword) ? keyword : null;

    public static bool TryGetPrimitiveInfo(string keyword, out PrimitiveSchemaMap.Info info)
    {
        if (s_keywordToType.TryGetValue(keyword, out var clrType))
            return PrimitiveSchemaMap.TryGet(clrType, out info);

        info = default;
        return false;
    }
}

using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Core;

/// <summary>
/// Shared lookup from a CLR primitive type to the <see cref="JsonSchemaType"/>
/// + <c>format</c> pair its wire schema uses.
/// </summary>
public static class PrimitiveSchemaMap
{
    public readonly record struct Info(JsonSchemaType Type, string? Format);

    private static readonly Dictionary<Type, Info> s_byType = new()
    {
        [typeof(sbyte)]   = new Info(JsonSchemaType.Integer, "int32"),
        [typeof(byte)]    = new Info(JsonSchemaType.Integer, "int32"),
        [typeof(short)]   = new Info(JsonSchemaType.Integer, "int32"),
        [typeof(ushort)]  = new Info(JsonSchemaType.Integer, "int32"),
        [typeof(int)]     = new Info(JsonSchemaType.Integer, "int32"),
        [typeof(uint)]    = new Info(JsonSchemaType.Integer, "int64"),
        [typeof(long)]    = new Info(JsonSchemaType.Integer, "int64"),
        [typeof(ulong)]   = new Info(JsonSchemaType.Integer, "int64"),
        [typeof(float)]   = new Info(JsonSchemaType.Number,  "float"),
        [typeof(double)]  = new Info(JsonSchemaType.Number,  "double"),
        [typeof(decimal)] = new Info(JsonSchemaType.Number,  "double"),
        [typeof(string)]  = new Info(JsonSchemaType.String,  null),
        [typeof(char)]    = new Info(JsonSchemaType.String,  null),
        [typeof(bool)]    = new Info(JsonSchemaType.Boolean, null),
    };

    public static bool TryGet(Type clrType, out Info info) => s_byType.TryGetValue(clrType, out info);
}

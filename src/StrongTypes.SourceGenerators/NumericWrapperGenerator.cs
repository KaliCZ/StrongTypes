using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StrongTypes.SourceGenerators;

[Generator]
public sealed class NumericWrapperGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "StrongTypes.NumericWrapperAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFullName,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => Model.From(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(targets, static (spc, model) =>
        {
            spc.AddSource($"{model.HintName}.g.cs", SourceText.From(Emitter.EmitType(model), Encoding.UTF8));
            spc.AddSource($"{model.HintName}.Extensions.g.cs", SourceText.From(Emitter.EmitExtensions(model), Encoding.UTF8));
        });
    }

    internal sealed record Model(
        string Namespace,
        string TypeName,
        string TypeNameWithArity,
        string SelfType,
        string UnderlyingType,
        bool UnderlyingIsValueType,
        string? TypeParameterList,
        ImmutableArray<string> ConstraintClauses,
        string AccessModifier,
        string InvariantDescription,
        bool GenerateSum,
        string HintName)
    {
        public static Model? From(GeneratorAttributeSyntaxContext ctx)
        {
            if (ctx.TargetSymbol is not INamedTypeSymbol symbol)
                return null;

            var valueProperty = symbol.GetMembers("Value")
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic);

            if (valueProperty is null)
                return null;

            var underlyingType = valueProperty.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var underlyingIsValueType = valueProperty.Type.IsValueType
                && valueProperty.Type.TypeKind != TypeKind.TypeParameter;
            var ns = symbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : symbol.ContainingNamespace.ToDisplayString();

            var typeName = symbol.Name;
            string? typeParameterList = null;
            string selfType = typeName;
            string typeNameWithArity = typeName;
            if (symbol.IsGenericType)
            {
                var parameters = string.Join(", ", symbol.TypeParameters.Select(tp => tp.Name));
                typeParameterList = $"<{parameters}>";
                selfType = $"{typeName}<{parameters}>";
                typeNameWithArity = $"{typeName}`{symbol.TypeParameters.Length}";
            }

            var constraintClauses = BuildConstraintClauses(symbol);

            var attr = ctx.Attributes[0];
            string invariantDescription = "valid";
            bool generateSum = false;

            foreach (var kvp in attr.NamedArguments)
            {
                switch (kvp.Key)
                {
                    case "InvariantDescription" when kvp.Value.Value is string s:
                        invariantDescription = s;
                        break;
                    case "GenerateSum" when kvp.Value.Value is bool b:
                        generateSum = b;
                        break;
                }
            }

            var accessibility = symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                _ => "internal"
            };

            var hintName = string.IsNullOrEmpty(ns)
                ? typeNameWithArity
                : $"{ns}.{typeNameWithArity}";

            return new Model(
                Namespace: ns,
                TypeName: typeName,
                TypeNameWithArity: typeNameWithArity,
                SelfType: selfType,
                UnderlyingType: underlyingType,
                UnderlyingIsValueType: underlyingIsValueType,
                TypeParameterList: typeParameterList,
                ConstraintClauses: constraintClauses,
                AccessModifier: accessibility,
                InvariantDescription: invariantDescription,
                GenerateSum: generateSum,
                HintName: hintName);
        }

        private static ImmutableArray<string> BuildConstraintClauses(INamedTypeSymbol symbol)
        {
            if (!symbol.IsGenericType)
                return ImmutableArray<string>.Empty;

            var builder = ImmutableArray.CreateBuilder<string>();
            foreach (var tp in symbol.TypeParameters)
            {
                var parts = new List<string>();

                if (tp.HasReferenceTypeConstraint)
                    parts.Add("class");
                else if (tp.HasValueTypeConstraint)
                    parts.Add("struct");
                else if (tp.HasUnmanagedTypeConstraint)
                    parts.Add("unmanaged");
                else if (tp.HasNotNullConstraint)
                    parts.Add("notnull");

                foreach (var c in tp.ConstraintTypes)
                    parts.Add(c.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

                if (tp.HasConstructorConstraint)
                    parts.Add("new()");

                if (parts.Count == 0)
                    continue;

                builder.Add($"where {tp.Name} : {string.Join(", ", parts)}");
            }
            return builder.ToImmutable();
        }
    }

    internal static class Emitter
    {
        public static string EmitType(Model m)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(m.Namespace))
            {
                sb.Append("namespace ").Append(m.Namespace).AppendLine(";");
                sb.AppendLine();
            }

            var self = m.SelfType;
            var t = m.UnderlyingType;

            sb.Append("partial struct ").Append(m.TypeName);
            if (m.TypeParameterList is not null)
                sb.Append(m.TypeParameterList);
            sb.AppendLine(" :");
            sb.Append("    global::System.IEquatable<").Append(self).AppendLine(">,");
            sb.Append("    global::System.IEquatable<").Append(t).AppendLine(">,");
            sb.Append("    global::System.IComparable<").Append(self).AppendLine(">,");
            sb.Append("    global::System.IComparable<").Append(t).AppendLine(">,");
            sb.AppendLine("    global::System.IComparable");

            foreach (var c in m.ConstraintClauses)
                sb.Append("    ").AppendLine(c);

            sb.AppendLine("{");

            sb.Append("    public static implicit operator ").Append(t).Append('(').Append(self).AppendLine(" value) => value.Value;");
            sb.Append("    public static explicit operator ").Append(self).Append('(').Append(t).AppendLine(" value) => Create(value);");
            sb.AppendLine();

            sb.Append("    public static ").Append(self).Append(" Create(").Append(t).AppendLine(" value)");
            sb.Append("        => TryCreate(value) ?? throw new global::System.ArgumentException($\"Value must be ")
              .Append(m.InvariantDescription)
              .AppendLine(", but was '{value}'.\", nameof(value));");
            sb.AppendLine();

            sb.AppendLine("    public override int GetHashCode() => Value.GetHashCode();");
            sb.AppendLine();

            sb.AppendLine("    public override bool Equals(object? obj) => obj switch");
            sb.AppendLine("    {");
            sb.Append("        ").Append(self).AppendLine(" other => Equals(other),");
            sb.Append("        ").Append(t).AppendLine(" other => Equals(other),");
            sb.AppendLine("        _ => false");
            sb.AppendLine("    };");
            sb.AppendLine();

            sb.Append("    public bool Equals(").Append(self).AppendLine(" other) => Value.Equals(other.Value);");
            if (m.UnderlyingIsValueType)
                sb.Append("    public bool Equals(").Append(t).AppendLine(" other) => Value.Equals(other);");
            else
                sb.Append("    public bool Equals(").Append(t).AppendLine("? other) => other is not null && Value.Equals(other);");
            sb.AppendLine();

            sb.Append("    public static bool operator ==(").Append(self).Append(" left, ").Append(self).AppendLine(" right) => left.Equals(right);");
            sb.Append("    public static bool operator !=(").Append(self).Append(" left, ").Append(self).AppendLine(" right) => !left.Equals(right);");
            sb.Append("    public static bool operator ==(").Append(self).Append(" left, ").Append(t).AppendLine(" right) => left.Value.Equals(right);");
            sb.Append("    public static bool operator !=(").Append(self).Append(" left, ").Append(t).AppendLine(" right) => !left.Value.Equals(right);");
            sb.Append("    public static bool operator ==(").Append(t).Append(" left, ").Append(self).AppendLine(" right) => right.Value.Equals(left);");
            sb.Append("    public static bool operator !=(").Append(t).Append(" left, ").Append(self).AppendLine(" right) => !right.Value.Equals(left);");
            sb.AppendLine();

            sb.Append("    public int CompareTo(").Append(self).AppendLine(" other) => Value.CompareTo(other.Value);");
            if (m.UnderlyingIsValueType)
                sb.Append("    public int CompareTo(").Append(t).AppendLine(" other) => Value.CompareTo(other);");
            else
                sb.Append("    public int CompareTo(").Append(t).AppendLine("? other) => other is null ? 1 : Value.CompareTo(other);");
            sb.AppendLine();

            sb.AppendLine("    int global::System.IComparable.CompareTo(object? obj) => obj switch");
            sb.AppendLine("    {");
            sb.AppendLine("        null => 1,");
            sb.Append("        ").Append(self).AppendLine(" other => CompareTo(other),");
            sb.Append("        ").Append(t).AppendLine(" other => CompareTo(other),");
            sb.Append("        _ => throw new global::System.ArgumentException($\"Object must be of type ").Append(m.TypeName).Append(" or {typeof(").Append(t).AppendLine(").Name}.\", nameof(obj))");
            sb.AppendLine("    };");
            sb.AppendLine();

            foreach (var op in new[] { "<", "<=", ">", ">=" })
                sb.Append("    public static bool operator ").Append(op).Append('(').Append(self).Append(" left, ").Append(self).Append(" right) => left.CompareTo(right) ").Append(op).AppendLine(" 0;");
            foreach (var op in new[] { "<", "<=", ">", ">=" })
                sb.Append("    public static bool operator ").Append(op).Append('(').Append(self).Append(" left, ").Append(t).Append(" right) => left.Value.CompareTo(right) ").Append(op).AppendLine(" 0;");
            foreach (var op in new[] { "<", "<=", ">", ">=" })
                sb.Append("    public static bool operator ").Append(op).Append('(').Append(t).Append(" left, ").Append(self).Append(" right) => left.CompareTo(right.Value) ").Append(op).AppendLine(" 0;");
            sb.AppendLine();

            sb.AppendLine("    public override string ToString() => Value.ToString() ?? string.Empty;");

            sb.AppendLine("}");
            return sb.ToString();
        }

        public static string EmitExtensions(Model m)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(m.Namespace))
            {
                sb.Append("namespace ").Append(m.Namespace).AppendLine(";");
                sb.AppendLine();
            }

            var self = m.SelfType;
            var t = m.UnderlyingType;
            var className = $"{m.TypeName}Extensions";
            var methodTypeParams = m.TypeParameterList ?? string.Empty;
            var methodConstraints = m.ConstraintClauses.IsDefaultOrEmpty
                ? string.Empty
                : " " + string.Join(" ", m.ConstraintClauses);

            sb.Append(m.AccessModifier).Append(" static class ").AppendLine(className);
            sb.AppendLine("{");

            EmitUnwrap(sb, self, t, methodTypeParams, methodConstraints);
            sb.AppendLine();
            EmitMinMax(sb, "Min", "<", self, methodTypeParams, methodConstraints);
            sb.AppendLine();
            EmitMinMax(sb, "Max", ">", self, methodTypeParams, methodConstraints);

            if (m.GenerateSum)
            {
                sb.AppendLine();
                sb.Append("    public static ").Append(self).Append(" Sum").Append(methodTypeParams)
                  .Append("(this global::System.Collections.Generic.IEnumerable<").Append(self).Append("> values)").Append(methodConstraints).AppendLine();
                sb.AppendLine("    {");
                sb.AppendLine("        if (values is null) throw new global::System.ArgumentNullException(nameof(values));");
                sb.Append("        ").Append(t).AppendLine(" sum = default!;");
                sb.AppendLine("        foreach (var value in values)");
                sb.AppendLine("        {");
                sb.AppendLine("            sum = checked(sum + value.Value);");
                sb.AppendLine("        }");
                sb.Append("        return ").Append(self).AppendLine(".Create(sum);");
                sb.AppendLine("    }");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void EmitUnwrap(StringBuilder sb, string self, string underlying, string methodTypeParams, string methodConstraints)
        {
            // Marker method for EF Core translation: the translator rewrites this
            // call to access the underlying column directly. At runtime it just
            // returns Value, but the point is to make the LINQ intent translatable.
            sb.Append("    public static ").Append(underlying).Append(" Unwrap").Append(methodTypeParams)
              .Append("(this ").Append(self).Append(" value)").Append(methodConstraints).AppendLine(" => value.Value;");
        }

        private static void EmitMinMax(StringBuilder sb, string name, string op, string self, string methodTypeParams, string methodConstraints)
        {
            sb.Append("    public static ").Append(self).Append(' ').Append(name).Append(methodTypeParams)
              .Append("(this global::System.Collections.Generic.IEnumerable<").Append(self).Append("> values)").Append(methodConstraints).AppendLine();
            sb.AppendLine("    {");
            sb.AppendLine("        if (values is null) throw new global::System.ArgumentNullException(nameof(values));");
            sb.AppendLine("        using var e = values.GetEnumerator();");
            sb.AppendLine("        if (!e.MoveNext()) throw new global::System.InvalidOperationException(\"Sequence contains no elements.\");");
            sb.AppendLine("        var result = e.Current;");
            sb.AppendLine("        while (e.MoveNext())");
            sb.AppendLine("        {");
            sb.Append("            if (e.Current.CompareTo(result) ").Append(op).AppendLine(" 0) result = e.Current;");
            sb.AppendLine("        }");
            sb.AppendLine("        return result;");
            sb.AppendLine("    }");
        }
    }
}

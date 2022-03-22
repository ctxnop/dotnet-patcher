#region References
using Mono.Cecil;
using System.Text;
using System.Text.RegularExpressions;
#endregion

namespace DP.Compiling
{
	/// <summary>
	/// Resolve references on compilation.
	/// </summary>
	public static class CodeGenerator
	{
		#region Methods
		public static void AddReference(StringBuilder code, string refname)
		{
			code.AppendLine($"using {refname};");
		}

		public static void AddNamespace(StringBuilder code, string ns)
		{
			code.AppendLine($"namespace {ns} {{");
		}

		public static string GetCSharpTypeAlias(string typename)
		{
			if (string.CompareOrdinal(typename, "System.Void") == 0)	return "void";
			if (string.CompareOrdinal(typename, "System.Boolean") == 0)	return "bool";
			if (string.CompareOrdinal(typename, "System.Byte") == 0)	return "byte";
			if (string.CompareOrdinal(typename, "System.SByte") == 0)	return "sbyte";
			if (string.CompareOrdinal(typename, "System.Char") == 0)	return "char";
			if (string.CompareOrdinal(typename, "System.Decimal") == 0)	return "decimal";
			if (string.CompareOrdinal(typename, "System.Double") == 0)	return "double";
			if (string.CompareOrdinal(typename, "System.Single") == 0)	return "float";
			if (string.CompareOrdinal(typename, "System.Int32") == 0)	return "int";
			if (string.CompareOrdinal(typename, "System.IntPtr") == 0)	return "nint";
			if (string.CompareOrdinal(typename, "System.UIntPtr") == 0)	return "nuint";
			if (string.CompareOrdinal(typename, "System.Int64") == 0)	return "long";
			if (string.CompareOrdinal(typename, "System.UInt64") == 0)	return "long";
			if (string.CompareOrdinal(typename, "System.Int16") == 0)	return "short";
			if (string.CompareOrdinal(typename, "System.UInt16") == 0)	return "ushort";
			if (string.CompareOrdinal(typename, "System.String") == 0)	return "string";
			if (string.CompareOrdinal(typename, "System.Object") == 0)	return "object";
			return typename;
		}

		public static string GetTypeName(TypeReference td)
		{
			return GetCSharpTypeAlias(
				Regex.Replace(td.FullName, "`\\d+", "")	// Generics have marker with parameter count
				.Replace("/", ".")					// Nested type are int the form of "parent/child"
			);
		}

		public static void AddType(StringBuilder code, TypeDefinition td)
		{
			if (!string.IsNullOrWhiteSpace(td.Namespace))
				AddNamespace(code, td.Namespace);

			code.AppendLine($"public class {td.Name} {{");

			/* TODO: Add class members, either all or, if possible, detect used in provided code and declare those only
			// Add fields
			foreach(FieldDefinition fd in td.Fields)
			{
				// Skip backing fields
				if (fd.Name.EndsWith("k__BackingField")) continue;

				if (fd.IsPrivate) code.Append("private ");
				code.AppendLine($"{ Regex.Replace(fd.FieldType.FullName.Replace('/', '.'), "`\\d+", "") } {fd.Name};");
			}

			// Add properties
			foreach(PropertyDefinition pd in td.Properties)
			{
				code.Append($"{pd.PropertyType.FullName.Replace('/', '.')} {pd.Name} {{ ");
				if (pd.GetMethod != null)
				{
					if (pd.GetMethod.IsPrivate) code.Append("private ");
					code.Append("get; ");
				}

				if (pd.SetMethod != null)
				{
					if (pd.SetMethod.IsPrivate) code.Append("private ");
					code.Append("set; ");
				}

				code.AppendLine("}");
			}
			*/
		}

		public static void AddMethod(StringBuilder code, MethodDefinition md)
		{
			code.Append($"public ");
			code.Append($"{GetTypeName(md.ReturnType)} {md.Name} (");
			if (md.HasParameters)
			{
				uint count = 0;
				foreach(ParameterDefinition pd in md.Parameters)
				{
					if (!string.IsNullOrWhiteSpace(pd.ParameterType.Namespace))
						code.Append(pd.ParameterType.Namespace + ".");
					code.Append($"{pd.ParameterType.Name} {pd.Name}");
					count++;
					if (count < md.Parameters.Count) code.Append(", ");
				}
			}
			code.AppendLine(") {");
		}
		#endregion
	}
}
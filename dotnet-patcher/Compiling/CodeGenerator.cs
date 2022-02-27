#region References
using Mono.Cecil;
using System.Text;
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

		public static void AddType(StringBuilder code, TypeDefinition td)
		{
			if (!string.IsNullOrWhiteSpace(td.Namespace))
				AddNamespace(code, td.Namespace);

			code.AppendLine($"public class {td.Name} {{");
		}

		public static void AddMethod(StringBuilder code, MethodDefinition md)
		{
			code.Append($"public ");
			if (!string.IsNullOrWhiteSpace(md.ReturnType.Namespace))
				code.Append(md.ReturnType.Namespace + ".");
			code.Append($"{md.ReturnType.Name} {md.Name} (");
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
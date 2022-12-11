#region References
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Text;
using System;
#endregion

namespace DP.Patches
{
	/// <summary>
	/// Provides some extension method to help with Mono.Cecil.
	/// </summary>
	public static class DefinitionExtensions
	{
		/// <summary>
		/// Dump the IL code from a method.
		/// </summary>
		/// <param name="md">Method's definition.</param>
		/// <returns>The IL dump</returns>
		public static string DumpIl(this MethodDefinition md)
		{
			if (md == null) return string.Empty;
			StringBuilder sb = new StringBuilder();
			foreach(var item in md.Body.Instructions)
			{
				sb.AppendLine(item.ToString());
			}
			return sb.ToString();
		}

		/// <summary>
		/// Get a field reference by name.
		/// </summary>
		/// <param name="ilp">IL Processor.</param>
		/// <param name="name">Field's name.</param>
		/// <returns>The found field's reference or null if none is found.</returns>
		public static FieldReference FieldRef(this ILProcessor ilp, string name)
		{
			if (ilp == null) return null;
			foreach(FieldDefinition fd in ilp.Body.Method.DeclaringType.Fields)
			{
				if (string.CompareOrdinal(fd.Name, name) == 0)
				{
					return fd;
				}
			}
			return null;
		}

		/// <summary>
		/// Get a field reference by name.
		/// </summary>
		/// <param name="ilp">IL Processor.</param>
		/// <param name="name">Field's name.</param>
		/// <returns>The found field's reference or null if none is found.</returns>
		public static MethodReference MethodRef(this ILProcessor ilp, string name)
		{
			if (ilp == null) return null;
			foreach(MethodDefinition md in ilp.Body.Method.DeclaringType.Methods)
			{
				if (string.CompareOrdinal(md.Name, name) == 0)
				{
					return md;
				}
			}
			return null;
		}

		public delegate bool TypeMatcher(TypeDefinition td);
		public delegate bool MethodMatcher(MethodDefinition md);
		public delegate void MethodPatcher(ILProcessor ilp);

		/// <summary>
		/// Patch a method.
		/// </summary>
		/// <param name="asm">The assembly definition.</param>
		/// <param name="tm">A callback to match type definition.</param>
		/// <param name="mm">A callback to match method definition.</param>
		/// <param name="mp">A callback to patch the found methods.</param>
		public static void Patch(this AssemblyDefinition asm, TypeMatcher tm, MethodMatcher mm, MethodPatcher mp)
		{
			foreach(TypeDefinition td in asm.MainModule.Types)
			{
				if (tm(td))
				{
					foreach(MethodDefinition md in td.Methods)
					{
						if (mm(md))
						{
							// Call the patcher
							mp(md.Body.GetILProcessor());

							// Show patched code
							Console.WriteLine($" - patched: {md.FullName}");
						}
					}
				}
			}
		}
	}
}

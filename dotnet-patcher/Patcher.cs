#region References
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;
using System.Runtime.CompilerServices;
#endregion

namespace DP
{
	/// <summary>
	/// Base utility class for patching.
	/// </summary>
	public static class Patcher
	{
		#region Delegates
		/// <summary>
		/// A predicate to match a MethodDefinition.
		/// </summary>
		/// <param name="mr">The evaluated MethodDefinition.</param>
		/// <returns>true if matching, false otherwise.</returns>
		public delegate bool TypeDefinitionMatcher(TypeDefinition mr);

		/// <summary>
		/// A predicate to match a MethodDefinition.
		/// </summary>
		/// <param name="mr">The evaluated MethodDefinition.</param>
		/// <returns>true if matching, false otherwise.</returns>
		public delegate bool MethodDefinitionMatcher(MethodDefinition mr);

		/// <summary>
		/// A predicate to match a FieldDefinition.
		/// </summary>
		/// <param name="mr">The evaluated FieldDefinition.</param>
		/// <returns>true if matching, false otherwise.</returns>
		public delegate bool FieldDefinitionMatcher(FieldDefinition mr);
		#endregion

		#region Methods
		/// <summary>
		/// Find the AssemblyDefinition from the specified ILProcessor.
		/// </summary>
		/// <param name="ilp">The ILProcessor.</param>
		/// <returns>The AssemblyDefinition.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AssemblyDefinition GetAssemblyDefinition(ILProcessor ilp) { return ilp.Body.Method.DeclaringType.Module.Assembly; }

		/// <summary>
		/// Find the AssemblyDefinition from the specified MethodBody.
		/// </summary>
		/// <param name="mb">The MethodBody.</param>
		/// <returns>The AssemblyDefinition.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AssemblyDefinition GetAssemblyDefinition(MethodBody mb) { return mb.Method.DeclaringType.Module.Assembly; }

		/// <summary>
		/// Find the AssemblyDefinition from the specified MethodDefinition.
		/// </summary>
		/// <param name="md">The MethodDefinition.</param>
		/// <returns>The AssemblyDefinition.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AssemblyDefinition GetAssemblyDefinition(MethodDefinition md) { return md.DeclaringType.Module.Assembly; }

		/// <summary>
		/// Find the AssemblyDefinition from the specified TypeDefinition.
		/// </summary>
		/// <param name="td">The TypeDefinition.</param>
		/// <returns>The AssemblyDefinition.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AssemblyDefinition GetAssemblyDefinition(TypeDefinition td) { return td.Module.Assembly; }

		/// <summary>
		/// Find the AssemblyDefinition from the specified ModuleDefinition.
		/// </summary>
		/// <param name="md">The ModuleDefinition.</param>
		/// <returns>The AssemblyDefinition.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AssemblyDefinition GetAssemblyDefinition(ModuleDefinition md) { return md.Assembly; }

		/// <summary>
		/// Find the MethodDefinition from the specified ILProcessor.
		/// </summary>
		/// <param name="ilp">The ILProcessor.</param>
		/// <returns>The MethodDefinition.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MethodDefinition GetMethodDefinition(ILProcessor ilp) { return ilp.Body.Method; }

		/// <summary>
		/// Find the MethodDefinition from the specified MethodBody.
		/// </summary>
		/// <param name="mb">The MethodBody.</param>
		/// <returns>The MethodDefinition.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static MethodDefinition GetMethodDefinition(MethodBody mb) { return mb.Method; }

		/// <summary>
		/// Find the TypeDefinition from the specified ILProcessor.
		/// </summary>
		/// <param name="ilp">The ILProcessor.</param>
		/// <returns>The TypeDefinition.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TypeDefinition GetTypeDefinition(ILProcessor ilp) { return ilp.Body.Method.DeclaringType; }

		/// <summary>
		/// Find the TypeDefinition from the specified MethodBody.
		/// </summary>
		/// <param name="mb">The MethodBody.</param>
		/// <returns>The TypeDefinition.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TypeDefinition GetTypeDefinition(MethodBody mb) { return mb.Method.DeclaringType; }

		/// <summary>
		/// Find the TypeDefinition from the specified MethodDefinition.
		/// </summary>
		/// <param name="md">The MethodDefinition.</param>
		/// <returns>The TypeDefinition.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TypeDefinition GetTypeDefinition(MethodDefinition md) { return md.DeclaringType; }

		/// <summary>
		/// Find the first TypeDefinition matching the predicate.
		/// </summary>
		/// <param name="asm">The assembly to search in.</param>
		/// <param name="tdm">The predicate to match.</param>
		/// <returns>The TypeDefinition.</returns>
		public static TypeDefinition FindTypeDefinition(AssemblyDefinition asm, TypeDefinitionMatcher tdm)
		{
			foreach(ModuleDefinition md in asm.Modules)
			{
				foreach(TypeDefinition td in md.GetTypes())
				{
					if (tdm(td)) return td;
				}
			}
			return null;
		}

		/// <summary>
		/// Find the first MethodDefinition matching the predicate.
		/// </summary>
		/// <param name="td">The TypeDefinition to search in.</param>
		/// <param name="mdm">The predicate to match.</param>
		/// <returns>The MethodDefinition.</returns>
		public static MethodDefinition FindMethodDefinition(TypeDefinition td, MethodDefinitionMatcher mdm)
		{
			foreach(MethodDefinition md in td.Methods)
			{
				if (mdm(md)) return md;
			}
			return null;
		}

		/// <summary>
		/// Find the first FieldDefinition matching the predicate.
		/// </summary>
		/// <param name="td">The TypeDefinition to search in.</param>
		/// <param name="fdm">The predicate to match.</param>
		/// <returns>The FieldDefinition.</returns>
		public static FieldDefinition FindFieldDefinition(TypeDefinition td, FieldDefinitionMatcher fdm)
		{
			foreach(FieldDefinition fd in td.Fields)
			{
				if (fdm(fd)) return fd;
			}
			return null;
		}

		/// <summary>
		/// Find the first MethodDefinition matching the predicate.
		/// </summary>
		/// <param name="asm">The assembly to search in.</param>
		/// <param name="tdm">The predicate to match the declaring type.</param>
		/// <param name="mdm">The predicate to match the method.</param>
		/// <returns>The MethodDefinition.</returns>
		public static MethodDefinition FindMethodDefinition(AssemblyDefinition asm, TypeDefinitionMatcher tdm, MethodDefinitionMatcher mdm)
		{
			foreach(ModuleDefinition mod in asm.Modules)
			{
				foreach(TypeDefinition td in mod.GetTypes())
				{
					if (tdm(td))
					{
						foreach(MethodDefinition md in td.Methods)
						{
							if (mdm(md)) return md;
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Read an assembly from a file.
		/// </summary>
		/// <param name="filename">The file to read.</param>
		/// <returns>The resulting AssemblyDefinition.</returns>
		public static AssemblyDefinition ReadAssembly(string filepath)
		{
			return AssemblyDefinition.ReadAssembly(
				filepath,
				new ReaderParameters {
					AssemblyResolver = GetResolver(filepath)
				}
			);
		}

		/// <summary>
		/// Read an assembly from a MemoryStream.
		/// </summary>
		/// <param name="ms">The stream to read.</param>
		/// <param name="filename">The filepath it could have. Used to resolve dependencies.</param>
		/// <returns>The resulting AssemblyDefinition.</returns>
		public static AssemblyDefinition ReadAssembly(MemoryStream ms, string filepath)
		{
			return AssemblyDefinition.ReadAssembly(
				ms,
				new ReaderParameters {
					AssemblyResolver = GetResolver(filepath)
				}
			);
		}

		private static IAssemblyResolver GetResolver(string filename)
		{
			DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
			resolver.AddSearchDirectory(
				Path.GetDirectoryName(filename)
			);
			return resolver;
		}

		public static TypeDefinition T(this AssemblyDefinition ad, string name)
		{
			foreach(ModuleDefinition md in ad.Modules)
			{
				foreach(TypeDefinition td in md.Types)
				{
					if (string.CompareOrdinal(td.FullName, name) == 0) return td;
				}
			}
			return null;
		}

		public static TypeDefinition T(this TypeDefinition tdp, string name)
		{
			foreach(TypeDefinition td in tdp.NestedTypes)
			{
				if (string.CompareOrdinal(td.Name, name) == 0) return td;
			}
			return null;
		}

		public static FieldDefinition F(this TypeDefinition td, string name)
		{
			foreach(FieldDefinition fd in td.Fields)
			{
				if (string.CompareOrdinal(fd.Name, name) == 0) return fd;
			}
			return null;
		}

		public static PropertyDefinition P(this TypeDefinition td, string name)
		{
			foreach(PropertyDefinition pd in td.Properties)
			{
				if (string.Compare(pd.Name, name) == 0) return pd;
			}
			return null;
		}

		public static MethodDefinition M(this TypeDefinition td, string name)
		{
			foreach(MethodDefinition md in td.Methods)
			{
				if (string.CompareOrdinal(md.Name, name) == 0) return md;
			}
			return null;
		}
		#endregion
	}
}
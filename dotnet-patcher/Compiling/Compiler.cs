#region References
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#endregion

namespace DP.Compiling
{
	/// <summary>
	/// Resolve references on compilation.
	/// </summary>
	public static class Compiler
	{
		#region Methods
		/// <summary>
		/// Replace the specified method implementation with the specified code.
		/// </summary>
		/// <param name="ilp">The method to replace.</param>
		/// <param name="code">The CSharp code to compile and insert into this method.</param>
		public static void ReplaceMethod(ILProcessor ilp, string code, string extradefinitions = null)
		{
			SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(
				GeneratePatchCode(ilp, code, extradefinitions)
			);

			AssemblyDefinition asm = Patcher.GetAssemblyDefinition(ilp);
			using(MemoryStream dll = CompilePatchInMemory(asm, tree))
			{
				if (dll != null)
				{
					AssemblyDefinition asmPatch = Patcher.ReadAssembly(dll, asm.MainModule.FileName);
					MethodDefinition md = Patcher.FindMethodDefinition(
						asmPatch,
						(td) =>
							string.CompareOrdinal(td.FullName, Patcher.GetTypeDefinition(ilp).FullName) == 0,
						(md) =>
							string.CompareOrdinal(md.FullName, Patcher.GetMethodDefinition(ilp).FullName) == 0
					);

					ilp.Clear();
					foreach(Instruction i in md.Body.Instructions)
					{
						ilp.Append(i);
					}
				}
			}
		}

		public static void PrefixMethod(MethodDefinition method, string code, string extradefinitions = null)
		{
			ILProcessor ilp = method.Body.GetILProcessor();

			SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(
				GeneratePatchCode(ilp, code, extradefinitions)
			);

			AssemblyDefinition asm = Patcher.GetAssemblyDefinition(ilp);
			using(MemoryStream dll = CompilePatchInMemory(asm, tree))
			{
				if (dll != null)
				{
					AssemblyDefinition asmPatch = Patcher.ReadAssembly(dll, asm.MainModule.FileName);
					MethodDefinition md = Patcher.FindMethodDefinition(
						asmPatch,
						(td) =>
							string.CompareOrdinal(td.FullName, Patcher.GetTypeDefinition(ilp).FullName) == 0,
						(md) =>
							string.CompareOrdinal(md.FullName, Patcher.GetMethodDefinition(ilp).FullName) == 0
					);

					// Remove the final "return". If the method returns "void", then there is a final "ret" opcode to
					// remove. If the method returns something, then there is a ld-something opcode to remove to.
					md.Body.Instructions.RemoveAt(md.Body.Instructions.Count - 1);
					if (string.CompareOrdinal(md.ReturnType.FullName, "System.Void") != 0)
						md.Body.Instructions.RemoveAt(md.Body.Instructions.Count - 1);

					DP.Patches.DefinitionExtensions.DumpIl(md);

					Instruction start = ilp.Body.Instructions[0];
					foreach(Instruction i in md.Body.Instructions)
					{
						ilp.InsertBefore(start, RelocateIlOperand(i, asm));
					}
				}
			}
		}

		/// <summary>
		/// Resolve from the module the TypeDefinition corresponding to the TypeReference specified.
		/// </summary>
		/// <param name="module">The module to look into.</param>
		/// <param name="tr">The type to resolve.</param>
		/// <returns>The resolved TypeDefinition if found, null otherwise.</returns>
		private static TypeDefinition ResolveType(ModuleDefinition module, TypeReference tr)
		{
			if (tr.DeclaringType != null)
			{
				TypeDefinition tdParent = ResolveType(module, tr.DeclaringType);
				foreach(TypeDefinition td in tdParent.NestedTypes)
				{
					if (string.CompareOrdinal(td.FullName, tr.FullName) == 0) return td;
				}
				return null;
			}

			foreach(TypeDefinition td in module.Types)
			{
				if (string.CompareOrdinal(td.FullName, tr.FullName) == 0)
					return td;
			}
			return null;
		}

		/// <summary>
		/// Resolve from the module the EventDefinition corresponding to the EventReference specified.
		/// </summary>
		/// <param name="module">The module to look into.</param>
		/// <param name="er">The event to resolve.</param>
		/// <returns>The resolved EventDefinition if found, null otherwise.</returns>
		private static EventDefinition ResolveEvent(ModuleDefinition module, EventReference er)
		{
			TypeDefinition td = ResolveType(module, er.DeclaringType);
			if (td == null) return null;

			foreach(EventDefinition ed in td.Events)
			{
				if (string.CompareOrdinal(ed.FullName, er.FullName) == 0)
					return ed;
			}
			return null;
		}

		/// <summary>
		/// Resolve from the module the FieldDefinition corresponding to the FieldReference specified.
		/// </summary>
		/// <param name="module">The module to look into.</param>
		/// <param name="fd">The field to resolve.</param>
		/// <returns>The resolved FieldDefinition if found, null otherwise.</returns>
		private static FieldDefinition ResolveField(ModuleDefinition module, FieldReference fr)
		{
			TypeDefinition td = ResolveType(module, fr.DeclaringType);
			if (td == null) return null;

			foreach(FieldDefinition fd in td.Fields)
			{
				if (string.CompareOrdinal(fd.FullName, fr.FullName) == 0)
					return fd;
			}
			return null;
		}

		/// <summary>
		/// Resolve from the module the PropertyDefinition corresponding to the PropertyReference specified.
		/// </summary>
		/// <param name="module">The module to look into.</param>
		/// <param name="md">The property to resolve.</param>
		/// <returns>The resolved PropertyDefinition if found, null otherwise.</returns>
		private static PropertyDefinition ResolveProperty(ModuleDefinition module, PropertyReference pr)
		{
			TypeDefinition td = ResolveType(module, pr.DeclaringType);
			if (td == null) return null;

			foreach(PropertyDefinition pd in td.Properties)
			{
				if (string.CompareOrdinal(pd.FullName, pr.FullName) == 0)
					return pd;
			}
			return null;
		}

		/// <summary>
		/// Resolve from the module the MethodDefinition corresponding to the MethodReference specified.
		/// </summary>
		/// <param name="module">The module to look into.</param>
		/// <param name="md">The method to resolve.</param>
		/// <returns>The resolved MethodDefinition if found, null otherwise.</returns>
		private static MethodDefinition ResolveMethod(ModuleDefinition module, MethodReference mr)
		{
			TypeDefinition td = ResolveType(module, mr.DeclaringType);
			if (td == null) return null;

			foreach(MethodDefinition md in td.Methods)
			{
				if (string.CompareOrdinal(md.FullName, mr.FullName) == 0)
					return md;
			}
			return null;
		}

		private static IMemberDefinition ResolveMember(ModuleDefinition module, MemberReference member)
		{
			TypeReference tr = member as TypeReference;
			if (tr != null) return ResolveType(module, tr);

			EventReference er = member as EventReference;
			if (er != null) return ResolveEvent(module, er);

			FieldReference fr = member as FieldReference;
			if (fr != null) return ResolveField(module, fr);

			PropertyReference pr = member as PropertyReference;
			if (pr != null) return ResolveProperty(module, pr);

			MethodReference mr = member as MethodReference;
			if (mr != null) return ResolveMethod(module, mr);

			return null;
		}

		private static Instruction RelocateIlOperand(Instruction i, AssemblyDefinition asm)
		{
			// If no operand, there is no relocation to do
			if (i.Operand == null) return i;
			
			// If the operand isn't a IMemberDefinition, there is no relocation to do
			MemberReference mr = i.Operand as MemberReference;
			if (mr == null) return i;

			// Relocate the operand
			i.Operand = ResolveMember(asm.MainModule, mr);
			return i;
		}

		/// <summary>
		/// Generate the CSharp code for correct injection.
		/// </summary>
		/// <param name="ilp">The method to patch.</param>
		/// <param name="userCode">The patched implementation to inject.</param>
		/// <returns>The source code that will result in the correct implementation.</returns>
		private static string GeneratePatchCode(ILProcessor ilp, string userCode, string extradefinitions)
		{
			/* TODO: Generate code using SyntaxNode, print it like that:
			StringBuilder sb = new StringBuilder();
			using(StringWriter sw = new StringWriter(sb))
			{
				tree.GetRoot().NormalizeWhitespace().WriteTo(sw);
			}
			Console.WriteLine(sb.ToString());
			*/
			
			StringBuilder sb = new StringBuilder();
			
			CodeGenerator.AddReference(sb, "System");
			CodeGenerator.AddType(sb, Patcher.GetTypeDefinition(ilp));

			sb.AppendLine(extradefinitions);

			CodeGenerator.AddMethod(sb, Patcher.GetMethodDefinition(ilp));

			sb.AppendLine(userCode);
			sb.AppendLine("}");
			sb.AppendLine("}");
			if (!string.IsNullOrWhiteSpace(Patcher.GetTypeDefinition(ilp).Namespace))
				sb.AppendLine("}");

			Console.WriteLine(sb.ToString());

			return sb.ToString();
		}

		/// <summary>
		/// Get the Roslyn references needed to compile code to insert in the specified assembly.
		/// </summary>
		/// <param name="ad">The assembly to patch.</param>
		/// <returns>A list of references used to compile code for injection into the specified assembly.</returns>
		private static IList<MetadataReference> GetReferences(AssemblyDefinition ad)
		{
			List<MetadataReference> references = new List<MetadataReference>();
			foreach(ModuleDefinition mod in ad.Modules)
			{
				references.Add(MetadataReference.CreateFromFile(mod.FileName));
				string refPath = Path.GetDirectoryName(mod.FileName);
				foreach(AssemblyNameReference refAsm in mod.AssemblyReferences)
				{
					string asmPath = Path.Combine(refPath, refAsm.Name + ".dll");
					if (System.IO.File.Exists(asmPath))
					{
						references.Add(MetadataReference.CreateFromFile(asmPath));
					}
				}
			}
			return references;
		}

		/// <summary>
		/// Get the default CSharp compiler options.
		/// </summary>
		/// <returns>The default CSharp compiler options.</returns>
		private static CSharpCompilationOptions GetDefaultOptions()
		{
			return new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				optimizationLevel: OptimizationLevel.Release
			).WithSpecificDiagnosticOptions(
				new [] {
					new KeyValuePair<string, ReportDiagnostic>("CS1701", ReportDiagnostic.Suppress),
					new KeyValuePair<string, ReportDiagnostic>("CS0169", ReportDiagnostic.Suppress)
				}
			);
		}

		/// <summary>
		/// Create a compilation for the specified assembly with the specified parsed code.
		/// </summary>
		/// <param name="asm">Assembly to patch.</param>
		/// <param name="code">Parsed code.</param>
		/// <returns>The ready-to-emit compilation.</returns>
		private static CSharpCompilation CreatePatchingCompilation(AssemblyDefinition asm, SyntaxTree code)
		{
			return CSharpCompilation.Create(asm.Name + "-patched")
				.WithOptions(GetDefaultOptions())
				.AddReferences(GetReferences(asm))
				.AddSyntaxTrees(code);
		}
		
		/// <summary>
		/// Compile the specified patch code into an in-memory assembly.
		/// </summary>
		/// <param name="asm">The assembly to patch.</param>
		/// <param name="code">The code to inject.</param>
		/// <returns>A memory stream containing the patch method.</returns>
		private static MemoryStream CompilePatchInMemory(AssemblyDefinition asm, SyntaxTree code)
		{
			CSharpCompilation compiler = CreatePatchingCompilation(asm, code);
			MemoryStream ms = new MemoryStream();
			EmitResult result = compiler.Emit(ms);
			if (result.Success)
			{
				ms.Seek(0, SeekOrigin.Begin);
				return ms;
			}

			// TODO: handle compilation error in a better way than just dumping errors on console.
			foreach(Diagnostic d in result.Diagnostics)
			{
				Console.WriteLine(d.ToString());
			}
			return null;
		}
		#endregion
	}
}
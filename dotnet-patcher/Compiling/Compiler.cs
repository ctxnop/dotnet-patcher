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
		public static void ReplaceMethod(ILProcessor ilp, string code)
		{
			
			SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(
				GeneratePatchCode(ilp, code)
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

		/// <summary>
		/// Generate the CSharp code for correct injection.
		/// </summary>
		/// <param name="ilp">The method to patch.</param>
		/// <param name="userCode">The patched implementation to inject.</param>
		/// <returns>The source code that will result in the correct implementation.</returns>
		private static string GeneratePatchCode(ILProcessor ilp, string userCode)
		{
			StringBuilder sb = new StringBuilder();
			
			CodeGenerator.AddReference(sb, "System");
			CodeGenerator.AddType(sb, Patcher.GetTypeDefinition(ilp));
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
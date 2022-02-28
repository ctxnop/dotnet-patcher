#region References
using DP.Compiling;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
#endregion

namespace DP
{
	/// <summary>
	/// Main class.
	/// </summary>
	internal class Program
	{
		#region Variables
		private static List<IPatch> s_PatchList = new List<IPatch>();
		#endregion

		/// <summary>
		/// Load the patches.
		/// </summary>
		private static void LoadPatches()
		{
			if (s_PatchList.Count > 0) return;
			foreach(Type t in typeof(IPatch).Assembly.ExportedTypes)
			{
				if (!t.IsAbstract && typeof(IPatch).IsAssignableFrom(t))
				{
					s_PatchList.Add(Activator.CreateInstance(t) as IPatch);
				}
			}
		}

		/// <summary>
		/// Display the help message.
		/// </summary>
		static void PrintHelp()
		{
			Console.WriteLine(
				"Usage: \n"                                                    +
				"    dp <command>\n"                                           +
				"\n"                                                           +
				"Commands:\n"                                                  +
				"    help:  Display this help.\n"                              +
				"    dump:  Dump IL code of the specified definitions.\n"      +
				"        dp dump <Assembly> <Type or Member>\n"                +	
				"    patch: Apply a patch.\n"                                  +
				"        dp patch <Assembly> <PatchId>\n"                      +
				"        Patches:"
			);

			foreach(IPatch p in s_PatchList)
			{
				Console.WriteLine($"             - {p.Id}");
			}
		}

		/// <summary>
		/// Application's entry point.
		/// </summary>
		/// <param name="args">Arguments list.</param>
		/// <returns>An exit code, zero on success.</returns>
		static int Main(string[] args)
		{
			// Load the patches
			LoadPatches();

			// There must be at least one command.
			if (args.Length == 0)
			{
				PrintHelp();
				return -1;
			}

			// First param is supposed to be the command
			switch(args[0])
			{
				case "help":
					PrintHelp();
					return 0;
				case "dump":
					if (args.Length != 3)
					{
						Console.WriteLine("the 'dump' command expect exactly two parameters!");
						PrintHelp();
					}
					return Dump(args[1], args[2]);
				case "patch":
					if (args.Length != 3)
					{
						Console.WriteLine("the 'patch' command expect exactly two parameters!");
						PrintHelp();
						return -1;
					}
					return Patch(args[1], args[2]);
				default:
					Console.WriteLine($"Unknown command: {args[0]}");
					PrintHelp();
					return -1;
			}
		}

		/// <summary>
		/// Apply the specified patch on the specified assembly.
		/// </summary>
		/// <param name="sourceAssembly">The assembly to patch.</param>
		/// <param name="patchId">The Id to patch.</param>
		/// <returns>An exit code, zero on success.</returns>
		static int Patch(string sourceAssembly, string patchId)
		{
			UInt64 count = 0;
			
			// The specified assembly must exists
			if (!File.Exists(sourceAssembly))
			{
				Console.WriteLine($"The specified assembly to patch doesn't exists: {sourceAssembly}");
				return -1;
			}

			// If there is no backup, then create it
			if (!File.Exists(sourceAssembly + ".dporg"))
			{
				File.Copy(sourceAssembly, sourceAssembly + ".dporg");
			}

			// Load the assembly definition from the backup so that it's the original
			// unpatched version.
			AssemblyDefinition asm = Patcher.ReadAssembly(sourceAssembly  + ".dporg");

			// Apply all corresponding patches
			foreach(IPatch p in s_PatchList)
			{
				if (string.CompareOrdinal(p.Id, patchId) == 0)
				{
					if (p.Apply(asm) == false)
					{
						Console.WriteLine("Failed to apply a patch.");
						return -1;
					}
					count++;
				}
			}
			
			// If no patches were applied, then it's an error.
			if (count == 0)
			{
				Console.WriteLine($"No patch available with this ID: {patchId}");
				return -1;
			}

			asm.Write(sourceAssembly);
			Console.WriteLine($"{count} patch(s) applied!");
			return 0;
		}

		/// <summary>
		/// Dump as much informations as possible about the specified type or member.
		/// </summary>
		/// <param name="assemblyFile">The assembly to load.</param>
		/// <param name="what">What to dump infos on.</param>
		/// <returns>An exist code, zero on success.</returns>
		static int Dump(string assemblyFile, string what)
		{
			// The specified assembly must exists
			if (!File.Exists(assemblyFile))
			{
				Console.WriteLine($"The specified assembly doesn't exists: {assemblyFile}");
				return -1;
			}

			// If there is a backup, then use it instead the patched version.
			if (File.Exists(assemblyFile + ".dporg"))
				assemblyFile = assemblyFile + ".dporg";

			AssemblyDefinition asm = Patcher.ReadAssembly(assemblyFile);

			CompilationUnitSyntax cus = null;
			CodeGen.AddTypeDefinition(
				Patcher.FindTypeDefinition(
					asm,
					(td) => string.CompareOrdinal(td.Name, "SteamManager") == 0
				),
				ref cus
			);

			Console.WriteLine(CodeGen.ToString(cus));
			return 0;
		}
	}
}
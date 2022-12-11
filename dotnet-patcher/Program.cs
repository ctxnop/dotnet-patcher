#region References
using DP.Commands;
using DP.Utils;
using System;
using System.Collections.Generic;
#endregion

namespace DP
{
	/// <summary>
	/// Main class.
	/// </summary>
	internal class Program
	{
		/// <summary>
		/// Application's entry point.
		/// </summary>
		/// <param name="args">Arguments list.</param>
		/// <returns>An exit code, zero on success.</returns>
		static int Main(string[] args)
		{
			try
			{
				// If no arguments, display help and exit on error.
				if (args.Length == 0)
					throw new Exception("No command specified");

				ICommand cmd = Reflection.MakeFromName<ICommand>(args[0]);
				if (cmd == null)
					throw new Exception($"Unknown command: {args[0]}");

				// Add others arguments in command line
				List<string> options = new List<string>();
				for(int i = 1; i < args.Length; ++i)
					options.Add(args[i]);

				// Run the command
				return cmd.Run(options);
			}
			catch(Exception ex)
			{
				Console.Write("[");
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("ERROR");
				Console.ResetColor();
				Console.WriteLine("] " + ex.Message);
				(new HelpCommand()).Run(new List<string>());
				return -1;
			}
		}

/*
		/// <summary>
		/// Apply the specified patch on the specified assembly.
		/// </summary>
		/// <param name="patchId">The Id to patch.</param>
		/// <param name="sourceAssembly">The assembly to patch.</param>
		/// <returns>An exit code, zero on success.</returns>
		static int Patch(string patchId , string sourceAssembly)
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
*/
	}
}

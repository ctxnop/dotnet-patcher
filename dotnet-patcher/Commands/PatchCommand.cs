#region References
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using DP.Utils;
#endregion

namespace DP.Commands
{
	/// <summary>
	/// The 'patch' command.
	/// </summary>
	[DisplayName("patch"), Description("Patch an assembly.")]
	public class PatchCommand
		: ICommand
	{
		#region Methods
		/// <inheritdoc />
		public int Run(IList<string> args)
		{
			if (args.Count < 2) throw new ArgumentException("At least two arguments are required!");
			string assemblyFile = args[0];
			string product = args[1];

			if (!File.Exists(assemblyFile + ".dporg"))
				File.Copy(assemblyFile, assemblyFile + ".dporg");
			AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(assemblyFile + ".dporg");

			IPatch p = Reflection.MakeFromName<IPatch>(product);

			if (args.Count == 2)
			{
				// Apply all patches
				foreach(MethodInfo mi in p.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
				{
					DisplayNameAttribute n = Reflection.GetAttribute<DisplayNameAttribute>(mi);
					if (n != null)
					{
						Console.WriteLine($"Applying {product}:{n.DisplayName} patch on {assemblyFile}");
						mi.Invoke(p, new object[]{asm});
					}
				}
			}
			else
			{
				for(int i = 2; i < args.Count; ++i)
				{
					foreach(MethodInfo mi in p.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
					{
						DisplayNameAttribute n = Reflection.GetAttribute<DisplayNameAttribute>(mi);
						if (n != null && string.Compare(n.DisplayName, args[i]) == 0)
						{
							Console.WriteLine($"Applying {product}:{n.DisplayName} patch on {assemblyFile}");
							mi.Invoke(p, new object[]{asm});
						}
					}
				}
			}

			asm.Write(assemblyFile);
			return 0;
		}

		/// <inheritdoc />
		public void ShowHelp()
		{
			Console.Write("\t\t<");
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.Write("file");
			Console.ResetColor();
			Console.Write("> <");
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.Write("product");
			Console.ResetColor();
			Console.Write("> [");
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.Write("patch1 ... patchN");
			Console.ResetColor();
			Console.WriteLine("]\tApply alDisplay help of a specific command.");
			Console.WriteLine();
			Console.WriteLine("\t\tProducts:");

			foreach(Type t in Reflection.GetDerivedTypes<IPatch>())
			{
				DisplayNameAttribute name = Reflection.GetAttribute<DisplayNameAttribute>(t);
				DescriptionAttribute desc = Reflection.GetAttribute<DescriptionAttribute>(t);

				Console.Write("\t\t");
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.Write(name?.DisplayName);
				Console.ResetColor();
				Console.WriteLine("\t" + desc?.Description);

				foreach(MethodInfo mi in t.GetMethods(BindingFlags.Instance | BindingFlags.Public))
				{
					DisplayNameAttribute n = Reflection.GetAttribute<DisplayNameAttribute>(mi);
					DescriptionAttribute d = Reflection.GetAttribute<DescriptionAttribute>(mi);
					if (n != null && d != null)
					{
						Console.Write("\t\t\t");
						Console.ForegroundColor = ConsoleColor.Blue;
						Console.Write(n?.DisplayName);
						Console.ResetColor();
						Console.WriteLine("\t" + d?.Description);
					}
				}

				Console.WriteLine();
			}
			Console.WriteLine();
		}
		#endregion
	}
}

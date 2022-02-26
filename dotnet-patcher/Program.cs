#region References
using DP.Patches;
using Mono.Cecil;
using Mono.Collections.Generic;
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
                "Usage: \n"                                                        +
                "    dp <command>\n"                                               +
                "\n"                                                               +
                "Commands:\n"                                                      +
                "   help:  Display this help.\n"                                   +
                "   dump:  Dump IL code of the specified definitions.\n"           +
                "   patch: Apply a patch.\n"                                       +
                "       dp patch <file-to-patch> <patchname>\n"                    +
                "       Patches:"
            );

            foreach(IPatch p in s_PatchList)
            {
                Console.WriteLine($"           - {p.Id}");
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
                    Console.WriteLine("Not yet implemented!");
                    return -1;
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

            // Create a resolver
            DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(sourceAssembly));

            // Load the assembly definition from the backup so that it's the original
            // unpatched version.
            AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(
                sourceAssembly  + ".dporg",
                new ReaderParameters { AssemblyResolver = resolver }
            );

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
    }
}
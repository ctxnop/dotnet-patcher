#region References
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
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
        /// Find a definition by it's full name.
        /// </summary>
        /// <param name="list">The list in which the search is made.</param>
        /// <param name="name">The full name of the searched definition.</param>
        /// <typeparam name="T">The type of the definition.</typeparam>
        /// <returns>The found definition or null if no match.</returns>
        public static T Find<T>(this Collection<T> list, string name)
            where T : class, IMemberDefinition
        {
            if (list == null || string.IsNullOrWhiteSpace(name)) return null;
            foreach(T item in list)
            {
                if(string.CompareOrdinal(item.FullName, name) == 0)
                    return item;
            }
            return null;
        }

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
                            Console.WriteLine($"===== {md.FullName} =====");
                            Console.WriteLine("---- original ----");
                            Console.Write(md.DumpIl());

                            // Call the patcher
                            mp(md.Body.GetILProcessor());

                            // Show patched code
                            Console.WriteLine("---- patched ----");
                            Console.Write(md.DumpIl());
                        }
                    }
                }
            }
        }
    }
}
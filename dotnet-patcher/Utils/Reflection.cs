#region References
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
#endregion

namespace DP.Utils
{
	internal class Reflection
	{
		/// <summary>
		/// Get an attribute on a type.
		/// </summary>
		/// <typeparam name="T">Type of the attribute to get.</typeparam>
		/// <param name="t">Type on which the attribute will be searched.</param>
		/// <returns>The instance of the attribute if found, <c>null</c> otherwise.</returns>
		internal static T GetAttribute<T>(Type t) where T: Attribute
		{
			if (t == null) return null;
			object[] found = t.GetCustomAttributes(typeof(T), false);
			if (found == null || found.Length != 1) return null;
			return found[0] as T;
		}

		/// <summary>
		/// Get an attribute on a method.
		/// </summary>
		/// <typeparam name="T">Type of the attribute to get.</typeparam>
		/// <param name="mi">Method on which the attribute will be searched.</param>
		/// <returns>The instance of the attribute if found, <c>null</c> otherwise.</returns>
		internal static T GetAttribute<T>(MethodInfo mi) where T: Attribute
		{
			if (mi == null) return null;
			object[] found = mi.GetCustomAttributes(typeof(T), false);
			if (found == null || found.Length != 1) return null;
			return found[0] as T;
		}

		/// <summary>
		/// Get all known derived type from a base type.
		/// </summary>
		/// <typeparam name="T">The base type.</typeparam>
		/// <returns>A list of derived type.</returns>
		internal static IList<Type> GetDerivedTypes<T>() where T: class
		{
			List<Type> derived = new List<Type>();
			foreach(Type t in typeof(Program).Assembly.ExportedTypes)
			{
				if (!t.IsAbstract && typeof(T).IsAssignableFrom(t))
				{
					derived.Add(t);
				}
			}
			return derived;
		}

		/// <summary>
		/// Create an object instance based on a "T:System.DisplayNameAttribute".
		/// </summary>
		/// <typeparam name="T">The base type of the object</typeparam>
		/// <param name="name">Name to look for.</param>
		/// <returns>The instance created or <c>null</c> if not found.</returns>
		internal static T MakeFromName<T>(string name) where T: class
		{
			foreach(Type t in GetDerivedTypes<T>())
			{
				DisplayNameAttribute dn = GetAttribute<DisplayNameAttribute>(t);
				if (dn != null && string.CompareOrdinal(name, dn.DisplayName) == 0)
					return Activator.CreateInstance(t) as T;
			}
			return null;
		}
	}
}

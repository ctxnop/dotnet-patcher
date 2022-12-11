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
	}
}

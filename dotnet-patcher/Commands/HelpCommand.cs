#region References
using System;
using System.Collections.Generic;
using System.ComponentModel;
using DP.Utils;
#endregion

namespace DP.Commands
{
	/// <summary>
	/// The 'help' command.
	/// </summary>
	[DisplayName("help"), Description("Display the help message.")]
	public class HelpCommand
		: ICommand
	{
		#region Methods
		/// <inheritdoc />
		public int Run(IList<string> args)
		{
			Console.WriteLine("Usage: dp <command> [options]");
			Console.WriteLine("Commands:");
			if (args.Count == 0)
			{
				foreach(Type t in Reflection.GetDerivedTypes<ICommand>())
				{
					DisplayNameAttribute name = Reflection.GetAttribute<DisplayNameAttribute>(t);
					DescriptionAttribute desc = Reflection.GetAttribute<DescriptionAttribute>(t);

					Console.Write("\t");
					Console.ForegroundColor = ConsoleColor.Blue;
					Console.Write(name?.DisplayName);
					Console.ResetColor();
					Console.WriteLine("\t" + desc?.Description);

					ICommand cmd = Activator.CreateInstance(t) as ICommand;
					cmd.ShowHelp();
				}
			}
			else if (args.Count == 1)
			{
				ICommand cmd = Reflection.MakeFromName<ICommand>(args[0]);
				if (cmd == null) throw new Exception($"Unkown command: {args[0]}");
				cmd.ShowHelp();
			}
			else
			{
				throw new Exception("Too many arguments");
			}

			return 0;
		}

		/// <inheritdoc />
		public void ShowHelp()
		{
			Console.Write("\t\t[");
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.Write("command");
			Console.WriteLine("]\tDisplay help of a specific command.");
			Console.WriteLine();
		}
		#endregion
	}
}

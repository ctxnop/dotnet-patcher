#region References
using System.Collections.Generic;
#endregion

namespace DP.Commands
{
	/// <summary>
	/// Interface that all commands must implement.
	/// </summary>
	internal interface ICommand
	{
		#region Methods
		/// <summary>
		/// Run the command.
		/// </summary>
		/// <param name="args">List of arguments for the command</param>
		int Run(IList<string> args);

		/// <summary>
		/// Display help on the stdout.
		/// </summary>
		void ShowHelp();
		#endregion
	}
}

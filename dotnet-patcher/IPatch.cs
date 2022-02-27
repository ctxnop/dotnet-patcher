#region References
using Mono.Cecil;
#endregion

namespace DP
{
	/// <summary>
	/// All patches must implements this interface.
	/// </summary>
	public interface IPatch
	{
		#region Properties
		/// <summary>
		/// Get the patch Id.
		/// </summary>
		/// <value>An Id to reference the patch.</value>
		public string Id { get; }
		#endregion

		#region Methods
		/// <summary>
		/// Apply this patch on this assembly definition.
		/// </summary>
		/// <param name="asm">The assembly definition.</param>
		/// <returns>True if the patch is successfully applied. False otherwise.</returns>
		public bool Apply(AssemblyDefinition asm);
		#endregion
	}
}

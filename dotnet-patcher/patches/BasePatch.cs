#region References
using Mono.Cecil;
#endregion

namespace DP.Patches
{
    /// <summary>
    /// All patches must implements this interface.
    /// </summary>
    public abstract class BasePatch
        : IPatch
    {
        #region Properties
        /// <summary>
        /// Get the patch Id.
        /// </summary>
        /// <value>An Id to reference the patch.</value>
        public abstract string Id { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Apply this patch on this assembly definition.
        /// </summary>
        /// <param name="asm">The assembly definition.</param>
        /// <returns>True if the patch is successfully applied. False otherwise.</returns>
        public abstract bool Apply(AssemblyDefinition asm);
        #endregion
    }
}

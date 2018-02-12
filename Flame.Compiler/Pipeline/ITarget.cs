namespace Flame.Compiler.Pipeline
{
    /// <summary>
    /// A common interface for interacting with back-ends.
    /// </summary>
    public interface ITarget
    {
        /// <summary>
        /// Gets this target's name.
        /// </summary>
        /// <returns>The target's name.</returns>
        string Name { get; }

        /// <summary>
        /// Compiles an assembly content description to a target assembly. 
        /// </summary>
        /// <param name="contents">
        /// An assembly content description.
        /// </param>
        /// <returns>A target assembly.</returns>
        ITargetAssembly Compile(AssemblyContentDescription contents);
    }
}
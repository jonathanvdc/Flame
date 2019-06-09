namespace Flame.Llvm
{
    /// <summary>
    /// Describes common functionality implemented by name manglers.
    /// </summary>
    public abstract class NameMangler
    {
        /// <summary>
        /// Gets the given method's mangled name.
        /// </summary>
        /// <param name="method">The method whose name is to be mangled.</param>
        /// <param name="mangleFullName">
        /// If this is <c>true</c>, then the method's full name and signature is
        /// mangled. Otherwise, only its name is mangled.
        /// </param>
        /// <returns>The mangled name.</returns>
        public abstract string Mangle(IMethod method, bool mangleFullName);

        /// <summary>
        /// Gets the given field's mangled name.
        /// </summary>
        /// <param name="Field">The field whose name is to be mangled.</param>
        /// <param name="mangleFullName">
        /// If this is <c>true</c>, then the field's full name is
        /// mangled. Otherwise, only its name is mangled.
        /// </param>
        /// <returns>The mangled name.</returns>
        public abstract string Mangle(IField method, bool mangleFullName);

        /// <summary>
        /// Gets the given type's mangled name.
        /// </summary>
        /// <param name="type">The type whose name is to be mangled.</param>
        /// <param name="mangleFullName">
        /// If this is <c>true</c>, then the type's full name is
        /// mangled. Otherwise, only its name is mangled.
        /// </param>
        /// <returns>The mangled name.</returns>
        public abstract string Mangle(IType type, bool mangleFullName);
    }

    /// <summary>
    /// A name mangler implementation for C compatibility.
    /// Names are left neither mangled nor prefixed.
    /// </summary>
    public sealed class CMangler : NameMangler
    {
        private CMangler() { }

        /// <summary>
        /// An instance of a C name mangler.
        /// </summary>
        public static readonly CMangler Instance = new CMangler();

        /// <inheritdoc/>
        public override string Mangle(IMethod method, bool mangleFullName)
        {
            return method.Name.ToString();
        }

        /// <inheritdoc/>
        public override string Mangle(IField field, bool mangleFullName)
        {
            return field.Name.ToString();
        }

        /// <inheritdoc/>
        public override string Mangle(IType type, bool mangleFullName)
        {
            if (mangleFullName)
            {
                return type.FullName.ToString();
            }
            else
            {
                return type.Name.ToString();
            }
        }
    }
}

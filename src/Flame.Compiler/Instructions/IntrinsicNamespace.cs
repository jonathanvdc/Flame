using System;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// Represents a namespace of intrinsics.
    /// </summary>
    public struct IntrinsicNamespace
    {
        /// <summary>
        /// Creates an intrinsic namespace.
        /// </summary>
        /// <param name="ns">The namespace to manage.</param>
        public IntrinsicNamespace(string ns)
        {
            this.Namespace = ns;
        }

        /// <summary>
        /// Gets the name of the namespace of intrinsics.
        /// </summary>
        /// <value>The namespace, as a string.</value>
        public string Namespace { get; private set; }

        /// <summary>
        /// Tries to parse an intrinsic name as an intrinsic
        /// name in this namespace.
        /// </summary>
        /// <param name="intrinsicName">
        /// The intrinsic name to parse.
        /// </param>
        /// <param name="operatorName">
        /// The name of the operator specified by the intrinsic,
        /// if the intrinsic name is an namespaced intrinsic name.
        /// </param>
        /// <returns>
        /// <c>true</c> if the intrinsic name is an intrinsic name in
        /// the current namespace; otherwise, <c>false</c>.
        /// </returns>
        public bool TryParseIntrinsicName(
            string intrinsicName,
            out string operatorName)
        {
            // All namespaced intrinsics have the following name format:
            //
            //    <namespace>.<op>
            //
            var lastDot = intrinsicName.LastIndexOf('.');
            if (lastDot < 0 || intrinsicName.Substring(0, lastDot) != Namespace)
            {
                operatorName = null;
                return false;
            }
            else
            {
                operatorName = intrinsicName.Substring(lastDot + 1);
                return true;
            }
        }

        /// <summary>
        /// Parses an intrinsic name as a namespaced intrinsic name,
        /// assuming that the intrinsic name is a namespaced intrinsic
        /// name. Returns the name of the operator wrapped by the
        /// namespaced intrinsic name.
        /// </summary>
        /// <param name="intrinsicName">
        /// The namespaced intrinsic name to parse.
        /// </param>
        /// <returns>
        /// The operator name wrapped by the namespaced intrinsic name.
        /// </returns>
        public string ParseIntrinsicName(
            string intrinsicName)
        {
            string result;
            if (TryParseIntrinsicName(intrinsicName, out result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException(
                    $"Name '{intrinsicName}' is not an intrinsic name in the '{Namespace}' namespace.");
            }
        }

        /// <summary>
        /// Tests if an intrinsic name is a namespaced intrinsic name.
        /// </summary>
        /// <param name="intrinsicName">
        /// The intrinsic name to examine.
        /// </param>
        /// <returns>
        /// <c>true</c> if the intrinsic name is a namespaced intrinsic name;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool IsIntrinsicName(string intrinsicName)
        {
            string opName;
            return TryParseIntrinsicName(intrinsicName, out opName);
        }

        /// <summary>
        /// Creates a namespaced intrinsic name from a
        /// namespaced operator name.
        /// </summary>
        /// <param name="operatorName">
        /// The operator name to wrap in a namespaced intrinsic name.
        /// </param>
        /// <returns>
        /// A namespaced intrinsic name.
        /// </returns>
        public string GetIntrinsicName(string operatorName)
        {
            return Namespace + "." + operatorName;
        }

        /// <summary>
        /// Tests if an instruction prototype is a intrinsic prototype
        /// defined in the current namespace.
        /// </summary>
        /// <param name="prototype">
        /// The prototype to examine.
        /// </param>
        /// <returns>
        /// <c>true</c> if the prototype is a namespaced intrinsic prototype;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool IsIntrinsicPrototype(InstructionPrototype prototype)
        {
            return prototype is IntrinsicPrototype
                && IsIntrinsicName(((IntrinsicPrototype)prototype).Name);
        }

        /// <summary>
        /// Tests if an instruction prototype is a intrinsic prototype
        /// with a particular name defined in the current namespace.
        /// </summary>
        /// <param name="prototype">
        /// The prototype to examine.
        /// </param>
        /// <param name="name">
        /// The prototype name to expect.
        /// </param>
        /// <returns>
        /// <c>true</c> if the prototype is a namespaced intrinsic prototype
        /// with a name equal to <paramref name="name"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool IsIntrinsicPrototype(InstructionPrototype prototype, string name)
        {
            string opName;
            return prototype is IntrinsicPrototype
                && TryParseIntrinsicName(((IntrinsicPrototype)prototype).Name, out opName)
                && name == opName;
        }
    }
}

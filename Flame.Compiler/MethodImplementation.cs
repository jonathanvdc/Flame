using System.Collections.Generic;

namespace Flame.Compiler
{
    /// <summary>
    /// A method implementation: a method body represented as a
    /// control-flow graph along with a private copy of the return
    /// parameter, 'this' parameter and input parameters.
    /// </summary>
    public sealed class MethodImplementation
    {
        /// <summary>
        /// Creates a method implementation.
        /// </summary>
        /// <param name="returnParameter">
        /// The method implementation's return parameter.
        /// </param>
        /// <param name="thisParameter">
        /// The method implementation's 'this' parameter.
        /// </param>
        /// <param name="parameters">
        /// The method implementation's input parameters.
        /// </param>
        /// <param name="body">
        /// The method implementation's body, represented
        /// as a control-flow graph.
        /// </param>
        public MethodImplementation(
            Parameter returnParameter,
            Parameter thisParameter,
            IReadOnlyList<Parameter> parameters,
            FlowGraph body)
        {
            this.ReturnParameter = returnParameter;
            this.ThisParameter = thisParameter;
            this.Parameters = parameters;
            this.Body = body;
        }

        /// <summary>
        /// Gets the method implementation's return parameter.
        /// </summary>
        /// <returns>The return parameter.</returns>
        public Parameter ReturnParameter { get; private set; }

        /// <summary>
        /// Gets the method implementation's 'this' parameter, if any.
        /// </summary>
        /// <returns>The 'this' parameter.</returns>
        public Parameter ThisParameter { get; private set; }

        /// <summary>
        /// Gets the method implementation's input parameter list.
        /// </summary>
        /// <returns>The parameter list.</returns>
        public IReadOnlyList<Parameter> Parameters { get; private set; }

        /// <summary>
        /// Gets the control-flow graph that constitutes the method
        /// implementation's body.
        /// </summary>
        /// <returns>The method implementation.</returns>
        public FlowGraph Body { get; private set; }
    }
}
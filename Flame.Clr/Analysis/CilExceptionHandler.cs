using System.Collections.Generic;
using Flame.Compiler;

namespace Flame.Clr.Analysis
{
    /// <summary>
    /// Describes a CIL exception handler.
    /// </summary>
    public struct CilExceptionHandler
    {
        /// <summary>
        /// Creates an exception handler that will catch
        /// any exception.
        /// </summary>
        /// <param name="landingPad">
        /// The landing pad to redirect flow to when an exception gets thrown.
        /// </param>
        public CilExceptionHandler(
            BasicBlockTag landingPad)
            : this(landingPad, null)
        { }

        /// <summary>
        /// Creates an exception handler that will catch
        /// only exceptions that inherit from a list of types.
        /// </summary>
        /// <param name="landingPad">
        /// The landing pad to redirect flow to when an exception gets thrown.
        /// </param>
        /// <param name="handledExceptionTypes">
        /// The list of exception types that are handled.
        /// Subtypes of these types are also handled.
        /// </param>
        public CilExceptionHandler(
            BasicBlockTag landingPad,
            IReadOnlyList<IType> handledExceptionTypes)
        {
            this.LandingPad = landingPad;
            this.HandledExceptionTypes = handledExceptionTypes;
        }

        /// <summary>
        /// Gets the landing pad basic block to which flow is
        /// redirected when an exception is thrown.
        /// </summary>
        /// <value>A basic block tag.</value>
        public BasicBlockTag LandingPad { get; private set; }

        /// <summary>
        /// Gets the list of types supported by this exception handler.
        /// This property is <c>null</c> if the handler catches all exceptions.
        /// </summary>
        /// <value>A list of exception types, or <c>null</c>.</value>
        public IReadOnlyList<IType> HandledExceptionTypes { get; private set; }

        /// <summary>
        /// Tells if this exception handler will catch any exception.
        /// </summary>
        public bool IsCatchAll => HandledExceptionTypes == null;
    }
}

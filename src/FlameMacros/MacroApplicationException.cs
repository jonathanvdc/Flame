using System;
using System.Runtime.Serialization;
using Loyc.Syntax;

namespace FlameMacros
{
    /// <summary>
    /// An exception that is thrown when macro application fails.
    /// </summary>
    [Serializable]
    public class MacroApplicationException : Exception
    {
        public MacroApplicationException() { }
        public MacroApplicationException(string message) : base(message) { }
        public MacroApplicationException(string message, Exception inner) : base(message, inner) { }
        public MacroApplicationException(LNode at, string message) : this(message) { this.At = at; }

        public readonly LNode At;

        protected MacroApplicationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}

using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public interface IAnalyzedVariable : IVariable, IEquatable<IAnalyzedVariable>
    {
        /// <summary>
        /// Gets the variable's properties.
        /// </summary>
        IVariableProperties Properties { get; }
    }

    public interface IVariableProperties
    {
        /// <summary>
        /// Gets a boolean value that indicates whether the variable is local to the method, 
        /// i.e. it cannot be accessed by other methods without the use of a pointer, and goes out of scope when the method does.
        /// </summary>
        bool IsLocal { get; }
    }
}

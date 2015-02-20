using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    /// <summary>
    /// An enumeration of types of body statements which may be used in if/else, while, for... blocks.
    /// </summary>
    public enum BodyStatementType
    {
        /// <summary>
        /// A single statement.
        /// </summary>
        Single,
        /// <summary>
        /// A block statement.
        /// </summary>
        Block,
        /// <summary>
        /// An empty statement.
        /// </summary>
        Empty
    }
}

using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    /// <summary>
    /// Represents an object that can write markup nodes to an output.
    /// </summary>
    public interface INodeWriter
    {
        /// <summary>
        /// Writes the markup node to the designated output.
        /// </summary>
        /// <param name="Node"></param>
        void Write(IMarkupNode Node);
    }
}

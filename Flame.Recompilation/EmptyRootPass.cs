using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// A root pass that always returns an empty sequence of
    /// members.
    /// </summary>
    public sealed class EmptyRootPass : IPass<BodyPassArgument, IEnumerable<IMember>>
    {
        private EmptyRootPass() { }

        public static readonly EmptyRootPass Instance = new EmptyRootPass();

        public IEnumerable<IMember> Apply(BodyPassArgument Value)
        {
            return Enumerable.Empty<IMember>();
        }
    }
}

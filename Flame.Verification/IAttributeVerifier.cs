using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public interface IAttributeVerifier<in T>
        where T : IMember
    {
        bool Verify(IAttribute Attribute, T Member, ICompilerLog Log);
    }
}

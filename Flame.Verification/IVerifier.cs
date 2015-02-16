using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public interface IVerifier<in T>
    {
        bool Verify(T Member, ICompilerLog Log);
    }
}

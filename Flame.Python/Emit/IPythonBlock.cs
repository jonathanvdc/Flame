using Flame.Compiler;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public interface IPythonBlock : ICodeBlock, ISyntaxNode, IDependencyNode
    {
        IType Type { [Pure] get; }
    }

    public static class PythonEmitExtensions
    {
        public static void AddBodyCodeBuilder(this CodeBuilder Target, CodeBuilder Body)
        {
            if (Body.IsWhitespace)
            {
                Target.AddLine("pass");
            }
            else
            {
                Target.AddCodeBuilder(Body);
            }
        }
    }
}

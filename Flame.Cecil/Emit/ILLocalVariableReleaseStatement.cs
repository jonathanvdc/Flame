using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ILLocalVariableReleaseStatement : IStatement
    {
        public ILLocalVariableReleaseStatement(ILLocalVariable LocalVariable)
        {
            this.LocalVariable = LocalVariable;
        }

        public ILLocalVariable LocalVariable { get; private set; }

        public void Emit(IBlockGenerator Generator)
        {
            LocalVariable.Release();
        }

        public bool IsEmpty
        {
            get { return false; }
        }

        public IStatement Optimize()
        {
            return this;
        }
    }
}

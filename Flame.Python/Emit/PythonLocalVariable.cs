using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PythonLocalVariable : PythonVariableBase
    {
        public PythonLocalVariable(PythonCodeGenerator CodeGenerator, IVariableMember Member)
            : base(CodeGenerator)
        {
            this.Member = Member;
        }

        private string name;
        public string Name
        {
            get
            {
                if (name == null)
                {
                    if (string.IsNullOrWhiteSpace(Member.Name))
                    {
                        name = ((PythonCodeGenerator)CodeGenerator).GetLocalName(Member.VariableType);
                    }
                    else
                    {
                        name = ((PythonCodeGenerator)CodeGenerator).GetLocalName(Member.Name);
                    }
                }
                return name;
            }
        }

        private void ReleaseName()
        {
            if (name != null)
            {
                ((PythonCodeGenerator)CodeGenerator).ReleaseLocalName(name);
            }
        }

        public IVariableMember Member { get; private set; }

        public override IPythonBlock CreateGetBlock()
        {
            return new PythonIdentifierBlock(this.CodeGenerator, Name, Type);
        }

        public override IStatement CreateReleaseStatement()
        {
            return new ReleaseLocalVariableStatement(this);
        }

        public override IType Type
        {
            get { return Member.VariableType; }
        }

        private class ReleaseLocalVariableStatement : IStatement
        {
            public ReleaseLocalVariableStatement(PythonLocalVariable Variable)
            {
                this.Variable = Variable;
            }

            public PythonLocalVariable Variable { get; private set; }

            public void Emit(IBlockGenerator Generator)
            {
                Variable.ReleaseName();
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
}

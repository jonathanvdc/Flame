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
                    if (string.IsNullOrWhiteSpace(Member.Name.ToString()))
                    {
                        name = ((PythonCodeGenerator)CodeGenerator).GetLocalName(Member.VariableType);
                    }
                    else
                    {
                        name = ((PythonCodeGenerator)CodeGenerator).GetLocalName(Member.Name.ToString());
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

        public override ICodeBlock EmitRelease()
        {
            return new ReleaseLocalVariableBlock(this);
        }

        public override IType Type
        {
            get { return Member.VariableType; }
        }

        private class ReleaseLocalVariableBlock : IPythonBlock
        {
            public ReleaseLocalVariableBlock(PythonLocalVariable Variable)
            {
                this.Variable = Variable;
            }

            public PythonLocalVariable Variable { get; private set; }

            public IType Type
            {
                get { return PrimitiveTypes.Void; }
            }

            public ICodeGenerator CodeGenerator
            {
                get { return Variable.CodeGenerator; }
            }

            public CodeBuilder GetCode()
            {
                Variable.ReleaseName();
                return new CodeBuilder();
            }

            public IEnumerable<ModuleDependency> GetDependencies()
            {
                return Enumerable.Empty<ModuleDependency>();
            }
        }
    }
}

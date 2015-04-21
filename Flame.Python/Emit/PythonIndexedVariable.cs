using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PythonIndexedVariable : PythonVariableBase
    {
        public PythonIndexedVariable(ICodeGenerator CodeGenerator, IPythonBlock Target, IPythonBlock[] Arguments)
            : base(CodeGenerator)
        {
            this.Target = Target;
            this.Arguments = Arguments;
        }

        public IPythonBlock Target { get; private set; }
        public IPythonBlock[] Arguments { get; private set; }

        public override IPythonBlock CreateGetBlock()
        {
            return new PythonIndexedBlock(CodeGenerator, Target, Arguments);
        }

        public override IType Type
        {
            get { return PythonObjectType.Instance; }
        }
    }

    public class PythonIndexedReleaseVariable : PythonIndexedVariable
    {
        public PythonIndexedReleaseVariable(ICodeGenerator CodeGenerator, IPythonBlock Target, IPythonBlock[] Arguments, IPythonBlock ReleaseStatement)
            : base(CodeGenerator, Target, Arguments)
        {
            this.ReleaseStatement = ReleaseStatement;
        }

        public IPythonBlock ReleaseStatement { get; private set; }

        public override ICodeBlock EmitRelease()
        {
            return ReleaseStatement;
        }
    }
}

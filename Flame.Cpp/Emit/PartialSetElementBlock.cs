using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class PartialSetElementBlock : IPartialBlock
    {
        public PartialSetElementBlock(ICppBlock Target, IType ElementType)
        {
            this.Target = Target;
            this.ElementType = ElementType;
        }

        public ICppBlock Target { get; private set; }
        public IType ElementType { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public ICppBlock Complete(PartialArguments Arguments)
        {
            Arguments.AssertCount(2);
            return new VariableAssignmentBlock(new ElementBlock(Target, Arguments.Get(0), Type), Arguments.Get(1));
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Target.Dependencies; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Target.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder(Target.GetCode());
            cb.Append("[] = ;");
            return cb;
        }
    }
}

using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ForBlock : ICppBlock, ICppLocalDeclaringBlock
    {
        public ForBlock(ICppBlock Initialization, ICppBlock Condition, ICppBlock Delta, ICppBlock Body)
        {
            this.Initialization = Initialization;
            this.Condition = Condition;
            this.Delta = Delta;
            this.Body = Body;
        }

        public ICppBlock Initialization { get; private set; }
        public ICppBlock Condition { get; private set; }
        public ICppBlock Delta { get; private set; }
        public ICppBlock Body { get; private set; }

        private ICppBlock[] AllBlocks
        {
            get { return new ICppBlock[] { Initialization, Condition, Delta, Body }; }
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return AllBlocks.GetDependencies(); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return AllBlocks.GetUsedLocals(); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Body.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("for (");
            cb.Append(Initialization.GetCode());
            cb.Append(" ");
            cb.Append(Condition.GetCode());
            cb.Append("; ");
            var deltaCode = Delta.GetCode();
            deltaCode.TrimEnd();
            deltaCode.TrimEnd(new char[] { ';' });
            cb.Append(deltaCode);
            cb.Append(')');
            cb.AddBodyCodeBuilder(Body.GetCode());
            return cb;
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return AllBlocks.GetLocalDeclarations(); }
        }
    }
}

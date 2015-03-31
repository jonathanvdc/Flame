using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class WhileBlock : ICppBlock, ICppLocalDeclaringBlock
    {
        public WhileBlock(ICppBlock Condition, ICppBlock Body)
        {
            this.Condition = Condition;
            this.Body = Body;
        }

        public ICppBlock Condition { get; private set; }
        public ICppBlock Body { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Condition.Dependencies.MergeDependencies(Body.Dependencies); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Condition.LocalsUsed.Union(Body.LocalsUsed); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Body.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            cb.Append("while (");
            cb.Append(Condition.GetCode());
            cb.Append(')');
            cb.AppendLine();
            cb.AddBodyCodeBuilder(Body.GetCode());
            return cb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return Condition.GetLocalDeclarations().Concat(Body.GetLocalDeclarations()); }
        }
    }
}

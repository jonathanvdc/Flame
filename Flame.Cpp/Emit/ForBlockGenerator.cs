using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ForBlockGenerator : CppBlockGeneratorBase
    {
        public ForBlockGenerator(CppCodeGenerator CodeGenerator, ICppBlock Initialization, ICppBlock Condition, ICppBlock Delta)
            : base(CodeGenerator)
        {
            this.Initialization = Initialization;
            this.Condition = Condition;
            this.Delta = Delta;
            foreach (var item in Delta.GetLocalDeclarations())
            {
                if (Initialization.DeclaresLocal(item.Local))
                {
                    item.DeclareVariable = false;
                }
            }
        }

        public ICppBlock Initialization { get; private set; }
        public ICppBlock Condition { get; private set; }
        public ICppBlock Delta { get; private set; }

        protected IEnumerable<ICppBlock> HeaderBlocks { get { return new ICppBlock[] { Initialization, Condition, Delta }; } }

        public override IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                return HeaderBlocks.Aggregate(Enumerable.Empty<IHeaderDependency>(), (a, b) => a.MergeDependencies(b.Dependencies)).MergeDependencies(base.Dependencies);
            }
        }

        public override CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
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
            cb.AddBodyCodeBuilder(base.GetCode());
            return cb;
        }

        public override IEnumerable<CppLocal> LocalsUsed
        {
            get { return HeaderBlocks.SelectMany(item => item.LocalsUsed).Concat(base.LocalsUsed).Distinct(); }
        }

        public override IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get
            {
                return HeaderBlocks.GetLocalDeclarations().Concat(base.LocalDeclarations);
            }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}

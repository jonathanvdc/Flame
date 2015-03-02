using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ForeachBlock : CppBlockGeneratorBase, IForeachBlockGenerator
    {
        public ForeachBlock(CppCodeGenerator CodeGenerator, CollectionBlock Collection)
            : base(CodeGenerator)
        {
            this.Collection = Collection;
            DeclareElement();
        }

        public CollectionBlock Collection { get; private set; }
        public IReadOnlyList<IVariable> Elements { get { return new IVariable[] { elemDeclaration.Declaration.Local }; } }

        private LocalDeclarationReference elemDeclaration;

        private void DeclareElement()
        {
            var elemVar = (CppLocal)CodeGenerator.DeclareVariable(Collection.Member);
            this.elemDeclaration = new LocalDeclarationReference(elemVar);
        }

        public override IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                return base.Dependencies.MergeDependencies(Collection.Dependencies);
            }
        }

        public override IEnumerable<CppLocal> LocalsUsed
        {
            get
            {
                return base.LocalsUsed.Union(Collection.LocalsUsed);
            }
        }

        public override IEnumerable<LocalDeclarationReference> DeclarationBlocks
        {
            get
            {
                return base.DeclarationBlocks.Concat(new LocalDeclarationReference[] { elemDeclaration });
            }
        }

        public override CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("for (");
            cb.Append(elemDeclaration.GetExpressionCode(true));
            cb.Append(" : ");
            cb.Append(Collection.GetCode());
            cb.Append(")");
            cb.AddBodyCodeBuilder(base.GetCode());
            return cb;
        }
    }
}

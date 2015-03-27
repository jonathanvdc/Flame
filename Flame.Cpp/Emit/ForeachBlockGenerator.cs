using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ForeachBlockGenerator : CppBlockGeneratorBase, IForeachBlockGenerator
    {
        public ForeachBlockGenerator(CppCodeGenerator CodeGenerator, CollectionBlock Collection)
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
            var elemVar = (CppLocal)CppCodeGenerator.DeclareNewVariable(Collection.Member);
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

        public override ICppBlock Simplify()
        {
            return new ForeachBlock(elemDeclaration, Collection, base.Simplify());
        }
    }
}

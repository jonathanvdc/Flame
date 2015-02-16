using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ToReferenceBlock : CompositeBlockBase, IPointerBlock
    {
        public ToReferenceBlock(ICppBlock Target)
        {
            this.Target = Target;
        }

        public ICppBlock Target { get; private set; }

        public override IType Type
        {
            get { return Target.Type.AsContainerType().GetElementType().MakePointerType(PointerKind.ReferencePointer); }
        }

        public override IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Target.Dependencies.MergeDependencies(new IHeaderDependency[] { StandardDependency.Memory }); }
        }

        public override IEnumerable<CppLocal> LocalsUsed
        {
            get { return Target.LocalsUsed; }
        }

        public override ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        protected override ICppBlock Simplify()
        {
            var method = CppPrimitives.CreateSharedPointer.MakeGenericMethod(new IType[] { Target.Type.AsContainerType().GetElementType() });
            var methodBlock = CodeGenerator.EmitMethod(method, null);
            return (ICppBlock)CodeGenerator.EmitInvocation(methodBlock, new ICodeBlock[] { Target });
        }

        public ICppBlock StaticDereference()
        {
            if (Target is IPointerBlock)
            {
                return ((IPointerBlock)Target).StaticDereference();
            }
            else
            {
                return new DereferenceBlock(Target);
            }
        }
    }
}

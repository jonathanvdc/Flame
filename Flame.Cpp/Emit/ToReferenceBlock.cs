using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ToReferenceBlock : CompositeBlockBase, IPointerBlock, INewObjectBlock
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

        protected bool CanUseMakeShared
        {
            get
            {
                return Target is INewObjectBlock && ((INewObjectBlock)Target).Kind == AllocationKind.UnmanagedHeap;
            }
        }

        protected override ICppBlock Simplify()
        {
            if (CanUseMakeShared)
            {
                var innerCtor = (INewObjectBlock)Target;
                var method = CppPrimitives.GetMakeSharedPointerMethod(Target.Type.AsContainerType().GetElementType(), innerCtor.Arguments.Select((item) => item.Type));
                var methodBlock = CodeGenerator.EmitMethod(method, null);
                return (ICppBlock)CodeGenerator.EmitInvocation(methodBlock, innerCtor.Arguments);
            }
            else
            {
                var method = CppPrimitives.CreateSharedPointer.MakeGenericMethod(new IType[] { Target.Type.AsContainerType().GetElementType() });
                var methodBlock = CodeGenerator.EmitMethod(method, null);
                return (ICppBlock)CodeGenerator.EmitInvocation(methodBlock, new ICodeBlock[] { Target });
            }
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

        public AllocationKind Kind
        {
            get
            {
                if (CanUseMakeShared)
                {
                    return AllocationKind.ManagedHeap;
                }
                else
                {
                    return AllocationKind.MakeManaged;
                }
            }
        }

        public IEnumerable<ICppBlock> Arguments
        {
            get
            {
                if (CanUseMakeShared)
                {
                    return ((INewObjectBlock)Target).Arguments;
                }
                else
                {
                    return new ICppBlock[] { Target };
                }
            }
        }
    }
}

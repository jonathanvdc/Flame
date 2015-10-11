using Flame.Build;
using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRTypeVisitor : TypeConverterBase<LNode>
    {
        public IRTypeVisitor(IRAssemblyBuilder Assembly)
        {
            this.Assembly = Assembly;
        }

        public IRAssemblyBuilder Assembly { get; private set; }

        protected override LNode ConvertTypeDefault(IType Type)
        {
            if (Type.DeclaringNamespace != null && Type.DeclaringNamespace.DeclaringAssembly != null)
            {
                Assembly.Dependencies.AddDependency(Type.DeclaringNamespace.DeclaringAssembly);
            }

            return NodeFactory.Call(IRParser.TypeReferenceName, new LNode[] { NodeFactory.Literal(Type.FullName) });
        }

        protected override LNode MakeArrayType(LNode ElementType, int ArrayRank)
        {
            return NodeFactory.Call(IRParser.ArrayTypeName, new LNode[] { ElementType, NodeFactory.Literal(ArrayRank) });
        }

        protected override LNode MakeGenericType(LNode GenericDeclaration, IEnumerable<LNode> TypeArguments)
        {
            return NodeFactory.Call(IRParser.GenericInstanceName, new LNode[] { GenericDeclaration }.Concat(TypeArguments));
        }

        protected override LNode MakePointerType(LNode ElementType, PointerKind Kind)
        {
            return NodeFactory.Call(IRParser.PointerTypeName, new LNode[] { ElementType, NodeFactory.Literal(Kind.Extension) });
        }

        protected override LNode MakeVectorType(LNode ElementType, IReadOnlyList<int> Dimensions)
        {
            return NodeFactory.Call(IRParser.VectorTypeName, new LNode[] { ElementType }.Concat(Dimensions.Select(item => NodeFactory.Literal(item))));
        }
    }
}

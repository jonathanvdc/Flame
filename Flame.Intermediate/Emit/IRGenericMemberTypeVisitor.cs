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
    public class IRGenericMemberTypeVisitor : IRTypeVisitor
    {
        public IRGenericMemberTypeVisitor(IRAssemblyBuilder Assembly, IGenericMember GenericMember)
            : base(Assembly)
        {
            this.GenericMember = GenericMember;
            this.Finder = new GenericParameterFinder(GenericMember);
        }

        public IGenericMember GenericMember { get; private set; }
        public GenericParameterFinder Finder { get; private set; }

        public override LNode GetTypeReference(IType Type)
        {
            if (Finder.Convert(Type))
            {
                return Convert(Type);
            }
            else
            {
                return base.GetTypeReference(Type);
            }
        }

        protected override LNode ConvertGenericParameter(IGenericParameter Type)
        {
            if (Type.DeclaringMember.Equals(GenericMember))
            {
                int index = 0;
                foreach (var item in GenericMember.GenericParameters)
	            {
                    if (item.Equals(Type))
                    {
                        return NodeFactory.Call(IRParser.LocalGenericParameterReferenceName, new LNode[]
                        {
                            NodeFactory.Literal(index)
                        });
                    }
                    index++;
	            }
            }
            return base.ConvertGenericParameter(Type);
        }
    }

    public class GenericParameterFinder : TypeConverterBase<bool>
    {
        public GenericParameterFinder(IGenericMember GenericMember)
        {
            this.GenericMember = GenericMember;
        }

        public IGenericMember GenericMember { get; private set; }

        protected override bool ConvertTypeDefault(IType Type)
        {
            return false;
        }

        protected override bool ConvertGenericParameter(IGenericParameter Type)
        {
            return Type.DeclaringMember.Equals(GenericMember) && 
                   GenericMember.GenericParameters.Any(item => item.Equals(Type));
        }

        protected override bool MakeArrayType(bool ElementType, int ArrayRank)
        {
            return ElementType;
        }

        protected override bool MakeGenericType(bool GenericDeclaration, IEnumerable<bool> TypeArguments)
        {
            return GenericDeclaration || TypeArguments.Any();
        }

        protected override bool MakePointerType(bool ElementType, PointerKind Kind)
        {
            return ElementType;
        }

        protected override bool MakeVectorType(bool ElementType, IReadOnlyList<int> Dimensions)
        {
            return ElementType;
        }

        protected override bool ConvertDelegateType(IType Type)
        {
            var method = MethodType.GetMethod(Type);

            return Convert(method.ReturnType) || Convert(method.Parameters.GetTypes()).Any();
        }

        protected override bool ConvertIntersectionType(IntersectionType Type)
        {
            return Convert(Type.First) || Convert(Type.Second);
        }
    }
}

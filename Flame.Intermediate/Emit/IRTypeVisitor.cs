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

        public LNode GetTypeReference(IType Type)
        {
            return Assembly.TypeTable.GetReference(Type);
        }

        protected override LNode ConvertTypeDefault(IType Type)
        {
            if (Type is GenericInstanceType)
            {
                return ConvertGenericNestedType((GenericInstanceType)Type);
            }

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

        protected virtual LNode MakePrimitiveType(string Name)
        {
            return NodeFactory.Id(Name);
        }

        protected virtual LNode MakePrimitiveType(string Name, int PrimitiveSize)
        {
            return NodeFactory.Call(Name, new LNode[] { NodeFactory.Literal(PrimitiveSize * 8) });
        }

        protected LNode MakePrimitiveType(string Name, IType Primitive)
        {
            return MakePrimitiveType(Name, Primitive.GetPrimitiveSize());
        }

        protected override LNode ConvertArrayType(IArrayType Type)
        {
            return MakeArrayType(GetTypeReference(Type.ElementType), Type.ArrayRank);
        }

        protected override LNode ConvertPointerType(IPointerType Type)
        {
            return MakePointerType(GetTypeReference(Type.ElementType), Type.PointerKind);
        }

        protected override LNode ConvertVectorType(IVectorType Type)
        {
            return MakeVectorType(GetTypeReference(Type.ElementType), Type.Dimensions);
        }

        protected override LNode ConvertGenericInstance(IType Type)
        {
            return MakeGenericType(GetTypeReference(Type.GetGenericDeclaration()), Type.GetGenericArguments().Select(GetTypeReference));
        }

        protected override LNode ConvertGenericParameter(IGenericParameter Type)
        {
            int index = 0;
            foreach (var item in Type.DeclaringMember.GenericParameters)
	        {
		        if (Type.Equals(item))
	            {
		            break;
	            }
                index++;
	        }
            if (Type.DeclaringMember is IMethod)
	        {
                return NodeFactory.Call(IRParser.MethodGenericParameterReferenceName, new LNode[]
                {
                    Assembly.MethodTable.GetReference((IMethod)Type.DeclaringMember),
                    NodeFactory.Literal(index)
                });
	        }
            else
            {
                return NodeFactory.Call(IRParser.TypeGenericParamaterReferenceName, new LNode[]
                {
                    GetTypeReference((IType)Type.DeclaringMember),
                    NodeFactory.Literal(index)
                });
            }
        }

        protected virtual LNode ConvertGenericNestedType(GenericInstanceType Type)
        {
            return NodeFactory.Call(IRParser.GenericInstanceMemberName, new LNode[] 
            { 
                GetTypeReference(Type.DeclaringType), 
                GetTypeReference(Type.Declaration) 
            });
        }

        protected override LNode ConvertNestedType(IType Type, IType DeclaringType)
        {
            return NodeFactory.Call(IRParser.NestedTypeName, new LNode[]
            {
                GetTypeReference(DeclaringType),
                NodeFactory.Literal(Type.Name)
            });
        }

        private static readonly Dictionary<IType, string> typeNames = new Dictionary<IType, string>()
        {
            { PrimitiveTypes.String, IRParser.StringTypeName },
            { PrimitiveTypes.Char, IRParser.CharTypeName },
            { PrimitiveTypes.Void, IRParser.VoidTypeName },
            { PrimitiveTypes.Boolean, IRParser.BooleanTypeName }
        };

        protected override LNode ConvertPrimitiveType(IType Type)
        {
            if (Type.get_IsSignedInteger())
            {
                return MakePrimitiveType(IRParser.IntTypeNodeName, Type);
            }
            else if (Type.get_IsUnsignedInteger())
            {
                return MakePrimitiveType(IRParser.UIntTypeNodeName, Type);
            }
            else if (Type.get_IsBit())
            {
                return MakePrimitiveType(IRParser.BitTypeNodeName, Type);
            }
            else if (Type.get_IsFloatingPoint())
            {
                return MakePrimitiveType(IRParser.FloatTypeNodeName, Type);
            }
            else if (typeNames.ContainsKey(Type))
            {
                return MakePrimitiveType(typeNames[Type]);
            }
            else
            {
                return base.ConvertPrimitiveType(Type);
            }
        }

        protected override LNode ConvertDelegateType(IType Type)
        {
            // Format:
            //
            // #delegate_type(return_type, parameter_types...)

            var signature = MethodType.GetMethod(Type);

            var retType = GetTypeReference(signature.ReturnType);
            var paramTypes = signature.Parameters.Select(item => GetTypeReference(item.ParameterType));

            return NodeFactory.Call(IRParser.DelegateTypeName, new LNode[] { retType }.Concat(paramTypes));
        }
    }
}

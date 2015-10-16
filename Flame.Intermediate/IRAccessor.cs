using Flame.Compiler;
using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRAccessor : INodeStructure<IAccessor>, IAccessor, IBodyMethod
    {
        public IRAccessor(IProperty DeclaringProperty, IRSignature Signature, AccessorType AccessorType, bool IsStatic, INodeStructure<IType> ReturnTypeNode)
        {
            this.DeclaringProperty = DeclaringProperty;
            this.Signature = Signature;
            this.AccessorType = AccessorType;
            this.IsStatic = IsStatic;
            this.ReturnTypeNode = ReturnTypeNode;
            this.ParameterNodes = EmptyNodeList<IParameter>.Instance;
            this.BaseMethodNodes = EmptyNodeList<IMethod>.Instance;
        }
        public IRAccessor(IProperty DeclaringProperty, IRSignature Signature, AccessorType AccessorType, bool IsStatic)
            : this(DeclaringProperty, Signature, AccessorType, IsStatic,
                   new ConstantNodeStructure<IType>(NodeFactory.Id(IRParser.VoidTypeName), PrimitiveTypes.Void))
        { }

        // Format:
        //
        // #accessor(#member(name, attrs...), accessor_type, is_static, return_type, { parameters... }, { base_methods... })
        //
        // --OR--
        //
        // #accessor(#member(name, attrs...), accessor_type, is_static, return_type, { parameters... }, { base_methods... }, body)


        public IProperty DeclaringProperty { get; private set; }
        public IRSignature Signature { get; set; }
        public AccessorType AccessorType { get; set; }
        public bool IsStatic { get; set; }
        public INodeStructure<IType> ReturnTypeNode { get; set; }
        public INodeStructure<IEnumerable<IParameter>> ParameterNodes { get; set; }
        public INodeStructure<IEnumerable<IMethod>> BaseMethodNodes { get; set; }
        public INodeStructure<IStatement> BodyNode { get; set; }

        public IType DeclaringType
        {
            get { return DeclaringProperty.DeclaringType; }
        }

        public bool IsConstructor
        {
            get { return false; }
        }

        public IEnumerable<IMethod> BaseMethods
        {
            get { return BaseMethodNodes.Value; }
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return null;
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return ParameterNodes.Value; }
        }

        public IType ReturnType
        {
            get { return ReturnTypeNode.Value; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Signature.Attributes; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); }
        }

        public string Name
        {
            get { return Signature.Name; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Enumerable.Empty<IGenericParameter>(); }
        }

        public IStatement GetMethodBody()
        {
            return BodyNode != null ? BodyNode.Value : null;
        }

        public const string AccessorNodeName = "#accessor";

        public LNode Node
        {
            get
            {
                var args = new List<LNode>()
                {
                    Signature.Node,
                    NodeFactory.IdOrLiteral(AccessorType.ToString()),
                    NodeFactory.Literal(IsStatic),
                    ReturnTypeNode.Node,
                    ParameterNodes.Node,
                    BaseMethodNodes.Node
                };

                if (BodyNode != null)
                {
                    args.Add(BodyNode.Node);
                }

                return NodeFactory.Call(AccessorNodeName, args);
            }
        }

        public IAccessor Value
        {
            get { return this; }
        }
    }
}

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
    public class IRMethod : INodeStructure<IMethod>, IMethod, IBodyMethod
    {
        public IRMethod(IType DeclaringType, IRSignature Signature, bool IsStatic, bool IsConstructor, INodeStructure<IType> ReturnTypeNode)
        {
            this.DeclaringType = DeclaringType;
            this.Signature = Signature;
            this.IsStatic = IsStatic;
            this.IsConstructor = IsConstructor;
            this.ReturnTypeNode = ReturnTypeNode;
            this.GenericParameterNodes = EmptyNodeList<IGenericParameter>.Instance;
            this.ParameterNodes = EmptyNodeList<IParameter>.Instance;
            this.BaseMethodNodes = EmptyNodeList<IMethod>.Instance;
        }
        public IRMethod(IType DeclaringType, IRSignature Signature, bool IsStatic, bool IsConstructor)
            : this(DeclaringType, Signature, IsStatic, IsConstructor,
                   new ConstantNodeStructure<IType>(NodeFactory.Id(IRParser.VoidTypeName), PrimitiveTypes.Void))
        { }

        // Format:
        //
        // #method(#member(name, attrs...), { generic_parameters... }, is_static, return_type, { parameters... }, { base_methods... })
        //
        // --OR--
        //
        // #method(#member(name, attrs...), { generic_parameters... }, is_static, return_type, { parameters... }, { base_methods... }, body)
        //
        // --OR--
        //
        // #ctor(#member(name, attrs...), { generic_parameters... }, is_static, return_type, { parameters... }, { base_methods... }, body)

        public IType DeclaringType { get; private set; }
        public IRSignature Signature { get; set; }
        public bool IsStatic { get; set; }
        public bool IsConstructor { get; set; }
        public INodeStructure<IEnumerable<IGenericParameter>> GenericParameterNodes { get; set; }
        public INodeStructure<IType> ReturnTypeNode { get; set; }
        public INodeStructure<IEnumerable<IParameter>> ParameterNodes { get; set; }
        public INodeStructure<IEnumerable<IMethod>> BaseMethodNodes { get; set; }
        public INodeStructure<IStatement> BodyNode { get; set; }

        public IEnumerable<IMethod> BaseMethods
        {
            get { return BaseMethodNodes.Value; }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return ParameterNodes.Value; }
        }

        public IType ReturnType
        {
            get { return ReturnTypeNode.Value; }
        }

        public AttributeMap Attributes
        {
            get { return Signature.Attributes; }
        }

        public QualifiedName FullName
        {
            get { return Name.Qualify(DeclaringType.FullName); }
        }

        public UnqualifiedName Name
        {
            get { return Signature.Name; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return GenericParameterNodes.Value; }
        }

        public IStatement GetMethodBody()
        {
            return BodyNode != null ? BodyNode.Value : null;
        }

        public const string MethodNodeName = "#method";
        public const string ConstructorNodeName = "#ctor";

        public LNode Node
        {
            get
            {
                var args = new List<LNode>()
                {
                    Signature.Node,
                    GenericParameterNodes.Node,
                    NodeFactory.Literal(IsStatic),
                    ReturnTypeNode.Node,
                    ParameterNodes.Node,
                    BaseMethodNodes.Node
                };

                if (BodyNode != null)
                {
                    args.Add(BodyNode.Node);
                }

                return NodeFactory.Call(IsConstructor ? ConstructorNodeName : MethodNodeName, args);
            }
        }

        public IMethod Value
        {
            get { return this; }
        }

        public override string ToString()
        {
            return FullName.ToString();
        }
    }
}

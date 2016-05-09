using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRField : INodeStructure<IField>, IField, IInitializedField
    {
        public IRField(IType DeclaringType, IRSignature Signature, bool IsStatic)
            : this(DeclaringType, Signature, IsStatic, new ConstantNodeStructure<IType>(NodeFactory.Id(IRParser.VoidTypeName), PrimitiveTypes.Void))
        { }
        public IRField(IType DeclaringType, IRSignature Signature, bool IsStatic, INodeStructure<IType> FieldTypeNode)
            : this(DeclaringType, Signature, IsStatic, FieldTypeNode, null)
        { }
        public IRField(IType DeclaringType, IRSignature Signature, bool IsStatic, INodeStructure<IType> FieldTypeNode, INodeStructure<IExpression> InitialValueNode)
        {
            this.DeclaringType = DeclaringType;
            this.Signature = Signature;
            this.IsStatic = IsStatic;
            this.FieldTypeNode = FieldTypeNode;
            this.InitialValueNode = InitialValueNode;
        }

        // Format:
        //
        // #field(#member(name, attrs...), is_static, field_type)
        //
        // --OR--
        //
        // #field(#member(name, attrs...), is_static, field_type, initial_value)

        public IType DeclaringType { get; private set; }
        public IRSignature Signature { get; set; }
        public bool IsStatic { get; set; }
        public INodeStructure<IType> FieldTypeNode { get; set; }
        public INodeStructure<IExpression> InitialValueNode { get; set; }

        public IType FieldType
        {
            get { return FieldTypeNode.Value; }
        }

        public AttributeMap Attributes
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

        public const string FieldNodeName = "#field";

        public LNode Node
        {
            get 
            {
                var args = new List<LNode>();
                args.Add(Signature.Node);
                args.Add(NodeFactory.Literal(IsStatic));
                args.Add(FieldTypeNode.Node);
                if (InitialValueNode != null)
                {
                    args.Add(InitialValueNode.Node);
                }
                return NodeFactory.Call(FieldNodeName, args);
            }
        }

        public IField Value
        {
            get { return this; }
        }

        public IExpression GetValue()
        {
            return InitialValueNode != null ? InitialValueNode.Value : null;
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}

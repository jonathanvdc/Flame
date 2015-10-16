using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRProperty : INodeStructure<IProperty>, IProperty
    {
        public IRProperty(IType DeclaringType, IRSignature Signature, bool IsStatic, INodeStructure<IType> PropertyTypeNode)
        {
            this.DeclaringType = DeclaringType;
            this.Signature = Signature;
            this.IsStatic = IsStatic;
            this.PropertyTypeNode = PropertyTypeNode;
            this.IndexerParameterNodes = EmptyNodeList<IParameter>.Instance;
            this.AccessorNodes = EmptyNodeList<IAccessor>.Instance;
        }
        public IRProperty(IType DeclaringType, IRSignature Signature, bool IsStatic)
            : this(DeclaringType, Signature, IsStatic, 
                   new ConstantNodeStructure<IType>(NodeFactory.Id(IRParser.VoidTypeName), PrimitiveTypes.Void))
        { }

        // Format:
        //
        // #property(#member(name, attrs...), is_static, property_type, { parameters... }, { accessors... })

        public IType DeclaringType { get; private set; }
        public IRSignature Signature { get; set; }
        public bool IsStatic { get; set; }
        public INodeStructure<IType> PropertyTypeNode { get; set; }
        public INodeStructure<IEnumerable<IParameter>> IndexerParameterNodes { get; set; }
        public INodeStructure<IEnumerable<IAccessor>> AccessorNodes { get; set; }

        public IEnumerable<IAccessor> Accessors
        {
            get { return AccessorNodes.Value; }
        }

        public IEnumerable<IParameter> IndexerParameters
        {
            get { return IndexerParameterNodes.Value; }
        }

        public IType PropertyType
        {
            get { return PropertyTypeNode.Value; }
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

        public const string PropertyNodeName = "#property";

        public LNode Node
        {
            get
            {
                return NodeFactory.Call(PropertyNodeName, new LNode[]
                {
                    Signature.Node,
                    NodeFactory.Literal(IsStatic),
                    PropertyTypeNode.Node,
                    IndexerParameterNodes.Node,
                    AccessorNodes.Node
                });
            }
        }

        public IProperty Value
        {
            get { return this; }
        }
    }
}

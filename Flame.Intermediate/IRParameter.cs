using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRParameter : IParameter, INodeStructure<IParameter>
    {
        public IRParameter(IRSignature Signature, INodeStructure<IType> ParameterTypeNode)
        {
            this.Signature = Signature;
            this.ParameterTypeNode = ParameterTypeNode;
        }

        public const string ParameterNodeName = "#param";

        // Format:
        //
        // #param(#member(name, attrs...), parameter_type)

        public IRSignature Signature { get; set; }
        public INodeStructure<IType> ParameterTypeNode { get; set; }

        public IType ParameterType
        {
            get { return ParameterTypeNode.Value; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Signature.Attributes; }
        }

        public string FullName
        {
            get { return Name; }
        }

        public string Name
        {
            get { return Signature.Name; }
        }

        public LNode Node
        {
            get
            {
                return NodeFactory.Call(ParameterNodeName, new LNode[]
                { 
                    Signature.Node,
                    ParameterTypeNode.Node
                });
            }
        }

        public IParameter Value
        {
            get { return this; }
        }
    }
}

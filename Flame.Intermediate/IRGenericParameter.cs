using Flame.Build;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRGenericParameter : IGenericParameter, INodeStructure<IGenericParameter>
    {
        public IRGenericParameter(IGenericMember DeclaringMember, IRSignature Signature)
        {
            this.DeclaringMember = DeclaringMember;
            this.Signature = Signature;
            this.GenericParameterNodes = EmptyNodeList<IGenericParameter>.Instance;
            this.ConstraintNodes = EmptyNodeList<IGenericConstraint>.Instance;
        }

        public const string GenericParameterNodeName = "#generic_parameter";

        // Format:
        //
        // #generic_parameter(#member(name, attributes...), { generic_parameters... }, { constraints... })

        public IGenericMember DeclaringMember { get; private set; }
        public IRSignature Signature { get; set; }
        public INodeStructure<IEnumerable<IGenericParameter>> GenericParameterNodes { get; set; }
        public INodeStructure<IEnumerable<IGenericConstraint>> ConstraintNodes { get; set; }

        public string Name
        {
            get { return Signature.Name; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringMember.FullName, Name); }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Signature.Attributes; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return GenericParameterNodes.Value; }
        }

        /// <summary>
        /// Gets all constraints that were inferred from the generic parameter's attributes.
        /// </summary>
        public IEnumerable<IGenericConstraint> InferredConstraints
        {
            get
            {
                var results = new List<IGenericConstraint>();
                foreach (var item in Attributes)
                {
                    if (item.AttributeType.Equals(PrimitiveAttributes.Instance.ReferenceTypeAttribute.AttributeType))
                    {
                        results.Add(ReferenceTypeConstraint.Instance);
                    }
                    else if (item.AttributeType.Equals(PrimitiveAttributes.Instance.ValueTypeAttribute.AttributeType))
                    {
                        results.Add(ValueTypeConstraint.Instance);
                    }
                    else if (item.AttributeType.Equals(PrimitiveAttributes.Instance.EnumAttribute.AttributeType))
                    {
                        results.Add(EnumConstraint.Instance);
                    }
                }
                return results;
            }
        }

        public IGenericConstraint Constraint
        {
            get { return new AndConstraint(InferredConstraints.Union(ConstraintNodes.Value)); }
        }

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }

        public INamespace DeclaringNamespace
        {
            get { return DeclaringMember as INamespace; }
        }

        public IEnumerable<IType> BaseTypes
        {
            get { return Constraint.ExtractBaseTypes(); }
        }

        public IEnumerable<IField> Fields
        {
            get { return Enumerable.Empty<IField>(); }
        }

        public IEnumerable<IMethod> Methods
        {
            get { return Enumerable.Empty<IMethod>(); }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return Enumerable.Empty<IProperty>(); }
        }

        public IBoundObject GetDefaultValue()
        {
            return null;
        }

        public IGenericParameter Value
        {
            get { return this; }
        }

        public LNode Node
        {
            get 
            {
                return NodeFactory.Call(GenericParameterNodeName, new LNode[]
                {
                    Signature.Node,
                    GenericParameterNodes.Node,
                    ConstraintNodes.Node
                });
            }
        }
    }
}

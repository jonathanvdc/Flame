using Flame.Intermediate.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public static class IREmitHelpers
    {
        public static IRSignature CreateSignature(IRAssemblyBuilder Assembly, string Name, IEnumerable<IAttribute> Attributes)
        {
            return new IRSignature(Name, Attributes.Select(item => ConvertAttribute(Assembly, item)));
        }

        #region Attributes

        public static INodeStructure<IAttribute> ConvertAttribute(IRAssemblyBuilder Assembly, IAttribute Attribute)
        {
            // TODO: implement this!
            throw new NotImplementedException();
        }

        #endregion

        #region Generics and constraints

        private static readonly Dictionary<IGenericConstraint, INodeStructure<IGenericConstraint>> constantConstraints = new Dictionary<IGenericConstraint, INodeStructure<IGenericConstraint>>()
        {
            { ReferenceTypeConstraint.Instance, 
              new ConstantNodeStructure<IGenericConstraint>(NodeFactory.Id(AttributeParsers.ReferenceTypeNodeName), ReferenceTypeConstraint.Instance) },
        
            { ValueTypeConstraint.Instance, 
              new ConstantNodeStructure<IGenericConstraint>(NodeFactory.Id(AttributeParsers.ValueTypeNodeName), ValueTypeConstraint.Instance) },
        };

        public static IEnumerable<INodeStructure<IGenericConstraint>> ConvertConstraint(IRAssemblyBuilder Assembly, IGenericConstraint Constraint)
        {
            if (Constraint is AndConstraint)
            {
                return ((AndConstraint)Constraint).Constraints.SelectMany(item => ConvertConstraint(Assembly, item));
            }
            else
            {
                INodeStructure<IGenericConstraint> constConstraint;
                if (constantConstraints.TryGetValue(Constraint, out constConstraint))
                {
                    return new INodeStructure<IGenericConstraint>[] { constConstraint };
                }
                else
                {
                    return Enumerable.Empty<INodeStructure<IGenericConstraint>>();
                }
            }
        }

        public static INodeStructure<IGenericParameter> ConvertGenericParameter(IRAssemblyBuilder Assembly, IGenericMember DeclaringMember, IGenericParameter GenericParameter)
        {
            var genParam = new IRGenericParameter(DeclaringMember, CreateSignature(Assembly, GenericParameter.Name, GenericParameter.Attributes));

            genParam.GenericParameterNodes = ConvertGenericParameters(Assembly, DeclaringMember, GenericParameter.GenericParameters);
            genParam.ConstraintNodes = new NodeList<IGenericConstraint>(ConvertConstraint(Assembly, GenericParameter.Constraint).ToArray());

            return genParam;
        }

        public static INodeStructure<IEnumerable<IGenericParameter>> ConvertGenericParameters(IRAssemblyBuilder Assembly, IGenericMember DeclaringMember, IEnumerable<IGenericParameter> GenericParameters)
        {
            return new NodeList<IGenericParameter>(GenericParameters.Select(item => ConvertGenericParameter(Assembly, DeclaringMember, item)).ToArray());
        }

        #endregion

        #region Parameters

        public static INodeStructure<IParameter> ConvertParameter(IRAssemblyBuilder Assembly, IParameter Parameter)
        {
            return new IRParameter(
                CreateSignature(Assembly, Parameter.Name, Parameter.Attributes), 
                Assembly.TypeTable.GetReferenceStructure(Parameter.ParameterType));
        }

        #endregion
    }
}

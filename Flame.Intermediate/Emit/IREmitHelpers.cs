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

        public static INodeStructure<IAttribute> ConvertAttribute(IRAssemblyBuilder Assembly, IAttribute Attribute)
        {
            // TODO: implement this!
            throw new NotImplementedException();
        }

        public static INodeStructure<IGenericParameter> ConvertGenericParameter(IRAssemblyBuilder Assembly, IGenericParameter GenericParameter)
        {
            // TODO: implement this!
            throw new NotImplementedException();
        }

        public static INodeStructure<IParameter> ConvertParameter(IRAssemblyBuilder Assembly, IParameter Parameter)
        {
            return new IRParameter(
                CreateSignature(Assembly, Parameter.Name, Parameter.Attributes), 
                Assembly.TypeTable.GetReferenceStructure(Parameter.ParameterType));
        }
    }
}

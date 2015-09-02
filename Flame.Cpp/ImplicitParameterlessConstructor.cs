using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class ImplicitParameterlessConstructor : IMethod
    {
        public ImplicitParameterlessConstructor(IType DeclaringType)
        {
            this.DeclaringType = DeclaringType;
        }

        public IType DeclaringType { get; private set; }

        public IEnumerable<IMethod> BaseMethods
        {
            get { return new IMethod[0]; }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return new IParameter[0]; }
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            throw new NotImplementedException();
        }

        public bool IsConstructor
        {
            get { return true; }
        }

        public IType ReturnType
        {
            get { return PrimitiveTypes.Void; }
        }

        public bool IsStatic
        {
            get { return false; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[] { new AccessAttribute(AccessModifier.Public), PrimitiveAttributes.Instance.ConstantAttribute }; }
        }

        public string Name
        {
            get { return DeclaringType.Name; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Enumerable.Empty<IGenericParameter>(); }
        }
    }
}

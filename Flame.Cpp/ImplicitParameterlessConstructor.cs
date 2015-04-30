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

        public IMethod[] GetBaseMethods()
        {
            return new IMethod[0];
        }

        public IMethod GetGenericDeclaration()
        {
            return this;
        }

        public IParameter[] GetParameters()
        {
            return new IParameter[0];
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            throw new NotImplementedException();
        }

        public bool IsConstructor
        {
            get { return true; }
        }

        public IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return this;
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

        public IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] { new AccessAttribute(AccessModifier.Public), PrimitiveAttributes.Instance.ConstantAttribute };
        }

        public string Name
        {
            get { return DeclaringType.Name; }
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return Enumerable.Empty<IType>();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Enumerable.Empty<IGenericParameter>();
        }
    }
}

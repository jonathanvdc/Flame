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

        public QualifiedName FullName
        {
            get { return Name.Qualify(DeclaringType.FullName); }
        }

        private static readonly AttributeMap attrMap = new AttributeMap(new IAttribute[] 
        { 
            new AccessAttribute(AccessModifier.Public), 
            PrimitiveAttributes.Instance.ConstantAttribute 
        });
        public AttributeMap Attributes
        {
            get { return attrMap; }
        }

        public UnqualifiedName Name
        {
            get { return DeclaringType.Name; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Enumerable.Empty<IGenericParameter>(); }
        }
    }
}

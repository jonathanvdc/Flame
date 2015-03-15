using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilAccessor : CecilMethodBase, IAccessor
    {
        public CecilAccessor(ICecilProperty DeclaringProperty, ICecilMethod Method, AccessorType AccessorType)
            : base((ICecilType)DeclaringProperty.DeclaringType)
        {
            this.DeclaringProperty = DeclaringProperty;
            this.Method = Method;
            this.AccessorType = AccessorType;
        }
        public CecilAccessor(ICecilProperty DeclaringProperty, MethodDefinition Method, AccessorType AccessorType)
            : this(DeclaringProperty, new CecilMethod((ICecilType)DeclaringProperty.DeclaringType, Method), AccessorType)
        {
        }

        public AccessorType AccessorType { get; private set; }
        public ICecilProperty DeclaringProperty { get; private set; }
        IProperty IAccessor.DeclaringProperty
        {
            get { return DeclaringProperty; }
        }
        public ICecilMethod Method { get; private set; }

        public override MethodReference GetMethodReference()
        {
            return Method.GetMethodReference();
        }

        public override IEnumerable<IType> GetGenericArguments()
        {
            return Method.GetGenericArguments();
        }

        public override IMethod GetGenericDeclaration()
        {
            return new CecilAccessor(DeclaringProperty, (ICecilMethod)Method.GetGenericDeclaration(), AccessorType);
        }

        public override IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return new CecilAccessor(DeclaringProperty, (ICecilMethod)Method.MakeGenericMethod(TypeArguments), AccessorType);
        }

        public override bool IsConstructor
        {
            get { return Method.IsConstructor; }
        }

        public override bool IsStatic
        {
            get { return Method.IsStatic; }
        }

        protected override IType ResolveLocalTypeParameter(IGenericParameter TypeParameter)
        {
            return Method.ResolveTypeParameter(TypeParameter);
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return Method.GetAttributes();
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            return new IAttribute[0];
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return new CustomAttribute[0];
        }

        public override IMethod[] GetBaseMethods()
        {
            return Method.GetBaseMethods();
        }
    }
}

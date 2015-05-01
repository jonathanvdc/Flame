using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilGenericInstanceMethod : CecilMethodBase
    {
        public CecilGenericInstanceMethod(ICecilType DeclaringType, ICecilMethod Method)
            : base(DeclaringType)
        {
            this.Method = Method;
        }

        public ICecilMethod Method { get; private set; }

        private MethodReference genRef;
        public override MethodReference GetMethodReference()
        {
            if (genRef == null)
            {
                genRef = Method.GetMethodReference().Reference(DeclaringType);
            }
            return genRef;
        }

        public override IMethod GetGenericDeclaration()
        {
            return this;
        }

        public override bool IsConstructor
        {
            get { return Method.IsConstructor; }
        }

        public override IMethod[] GetBaseMethods()
        {
            var results = Method.GetBaseMethods().Select(DeclaringType.ResolveMethod).ToArray();
            return results;
        }

        public override bool IsStatic
        {
            get { return Method.IsStatic; }
        }

        protected override IType ResolveLocalTypeParameter(IGenericParameter TypeParameter)
        {
            return null;
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            throw new InvalidOperationException();
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            throw new InvalidOperationException();
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return Method.GetAttributes();
        }

        public override string Name
        {
            get
            {
                return Method.Name;
            }
        }

        public override IType ReturnType
        {
            get
            {
                return this.ResolveType(Method.ReturnType);
            }
        }

        public override IParameter[] GetParameters()
        {
            return this.ResolveParameters(base.GetParameters());
        }

        public override IEnumerable<IType> GetGenericArguments()
        {
            return Enumerable.Empty<IType>();
        }

        public override IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Method.GetGenericParameters();
        }
    }
}

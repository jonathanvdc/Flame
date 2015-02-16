using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilMethod : CecilMethodBase, ICecilMethod
    {
        public CecilMethod(MethodReference Method)
            : this(CecilTypeBase.CreateCecil(Method.DeclaringType), Method)
        {
        }
        public CecilMethod(ICecilType DeclaringType, MethodReference Method)
            : base(DeclaringType)
        {
            this.Method = Method;
        }

        public MethodReference Method { get; private set; }

        public override MethodReference GetMethodReference()
        {
            return Method;
        }
        public MethodDefinition GetResolvedMethod()
        {
            return Method.Resolve();
        }

        public override IEnumerable<IType> GetCecilGenericArguments()
        {
            return new IType[0];
        }

        public override IMethod GetGenericDeclaration()
        {
            return this;
        }

        public override bool IsStatic
        {
            get { return GetResolvedMethod().IsStatic; }
        }

        public override bool IsConstructor
        {
            get { return GetResolvedMethod().IsConstructor; }
        }

        protected override IType ResolveLocalTypeParameter(IGenericParameter TypeParameter)
        {
            return null;
        }

        public AccessModifier Access
        {
            get
            {
                var resolvedMethod = GetResolvedMethod();
                if (resolvedMethod.IsPublic)
                {
                    return AccessModifier.Public;
                }
                else if (resolvedMethod.IsAssembly)
                {
                    return AccessModifier.Assembly;
                }
                else if (resolvedMethod.IsFamilyOrAssembly)
                {
                    return AccessModifier.ProtectedOrAssembly;
                }
                else if (resolvedMethod.IsFamily)
                {
                    return AccessModifier.Protected;
                }
                else if (resolvedMethod.IsFamilyAndAssembly)
                {
                    return AccessModifier.ProtectedAndAssembly;
                }
                else
                {
                    return AccessModifier.Private;
                }
            }
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            var resolvedMethod = GetResolvedMethod();
            List<IAttribute> attrs = new List<IAttribute>();
            attrs.Add(new AccessAttribute(Access));
            if (resolvedMethod.IsAbstract)
            {
                attrs.Add(PrimitiveAttributes.Instance.AbstractAttribute);
            }
            else if (resolvedMethod.IsVirtual && !resolvedMethod.IsFinal)
            {
                attrs.Add(PrimitiveAttributes.Instance.VirtualAttribute);
            }
            if (resolvedMethod.IsStatic && resolvedMethod.Name == "Concat" && resolvedMethod.Parameters.Count == 2 && resolvedMethod.DeclaringType.Equals(resolvedMethod.Module.TypeSystem.String))
            {
                attrs.Add(new OperatorAttribute(Operator.Concat));
            }
            return attrs;
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return GetResolvedMethod().CustomAttributes;
        }

        public override IMethod[] GetBaseMethods()
        {
            var cecilOverrides = GetResolvedMethod().Overrides;
            List<IMethod> overrides = new List<IMethod>(cecilOverrides.Count);
            for (int i = 0; i < cecilOverrides.Count; i++)
            {
                overrides.Add(CecilMethodBase.Create(cecilOverrides[i]));
            }
            if (this.DeclaringType.get_IsRootType())
            {
                if (this.Name == "GetHashCode")
                {
                    overrides.Add(PrimitiveMethods.Instance.GetHashCode);
                }
                else if (this.Name == "Equals")
                {
                    overrides.Add(PrimitiveMethods.Instance.Equals);
                }
            }
            
            return overrides.ToArray();
        }

        public override bool IsComplete
        {
            get { return true; }
        }
    }
}

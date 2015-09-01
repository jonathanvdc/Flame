using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilGenericMethod : CecilMethodBase
    {
        public CecilGenericMethod(ICecilType DeclaringType, ICecilMethod GenericDeclaration, IEnumerable<IType> TypeArguments)
            : base(DeclaringType)
        {
            this.GenericDeclaration = GenericDeclaration;
            this.TypeArguments = TypeArguments.ToArray();
            this.cachedParams = new Lazy<IParameter[]>(GetParametersCore);
            this.cachedReturnType = new Lazy<IType>(GetReturnTypeCore);
        }

        public ICecilMethod GenericDeclaration { get; private set; }
        public IType[] TypeArguments { get; private set; }

        private MethodReference methodRef;
        public override MethodReference GetMethodReference()
        {
            if (methodRef == null)
            {
                var genericMethodRef = GenericDeclaration.GetMethodReference();
                var cecilTypeArgs = TypeArguments.Select((item) => item.GetImportedReference(Module)).ToArray();
                if (cecilTypeArgs.All((item) => item != null))
                {
                    var inst = new GenericInstanceMethod(genericMethodRef);
                    foreach (var item in cecilTypeArgs)
                    {
                        inst.GenericArguments.Add(item); 
                    }
                    methodRef = inst;
                }
                else
                {
                    methodRef = genericMethodRef;
                }
            }
            return methodRef;
        }

        public override IEnumerable<IType> GetGenericArguments()
        {
            return TypeArguments;
        }

        public override IMethod GetGenericDeclaration()
        {
            return GenericDeclaration;
        }

        public override IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return GenericDeclaration.MakeGenericMethod(TypeArguments);
        }

        public override IMethod[] GetBaseMethods()
        {
            return GenericDeclaration.BaseMethods;
        }

        public override bool IsConstructor
        {
            get { return GenericDeclaration.IsConstructor; }
        }

        public override bool IsStatic
        {
            get { return GenericDeclaration.IsStatic; }
        }

        public override string Name
        {
            get
            {
                return GenericNameExtensions.ChangeTypeArguments(GenericDeclaration.Name, TypeArguments.Select(item => item.Name));
            }
        }

        public override string FullName
        {
            get
            {
                return GenericNameExtensions.ChangeTypeArguments(GenericDeclaration.FullName, TypeArguments.Select(item => item.FullName));
            }
        }

        private Lazy<IParameter[]> cachedParams;
        private Lazy<IType> cachedReturnType;

        private IParameter[] GetParametersCore()
        {
            return this.ResolveParameters(GenericDeclaration.GetParameters());
        }

        private IType GetReturnTypeCore()
        {
            return this.ResolveType(GenericDeclaration.ReturnType);
        }

        public override IParameter[] GetParameters()
        {
            return cachedParams.Value;
        }

        public override IType ReturnType
        {
            get
            {
                return cachedReturnType.Value;
            }
        }

        protected override IType ResolveLocalTypeParameter(IGenericParameter TypeParameter)
        {
            var genericParams = GenericDeclaration.GenericParameters.ToArray();
            for (int i = 0; i < genericParams.Length; i++)
            {
                if (genericParams[i].Equals(TypeParameter))
                {
                    return TypeArguments[i];
                }
            }
            string name = TypeParameter.Name;
            if (name.StartsWith("!!"))
            {
                int index = int.Parse(name.Substring(2));
                return TypeArguments[index];
            }
            return null;
        }

        #region Attributes

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            return null;
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return null;
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return GenericDeclaration.Attributes;
        }

        #endregion

        #region Equality/GetHashCode

        public override bool Equals(ICecilMethod other)
        {
            if (other is CecilGenericInstanceMethod)
            {
                return GenericDeclaration.Equals(other.GetGenericDeclaration()) && TypeArguments.AreEqual(other.GetGenericArguments().ToArray());
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}

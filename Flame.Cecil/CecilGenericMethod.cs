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
        public CecilGenericMethod(GenericInstanceMethod GenericInstance)
            : base(CecilTypeBase.CreateCecil(GenericInstance.DeclaringType))
        {
            this.GenericDeclaration = CecilMethodBase.Create(DeclaringType, GenericInstance.ElementMethod.Resolve());
            this.TypeArguments = GenericInstance.GenericArguments.Select((item) => CecilTypeBase.Create(item)).ToArray();
        }
        public CecilGenericMethod(ICecilType DeclaringType, GenericInstanceMethod GenericInstance)
            : base(DeclaringType)
        {
            this.GenericDeclaration = CecilMethodBase.Create(DeclaringType, GenericInstance.ElementMethod.Resolve());
            this.TypeArguments = GenericInstance.GenericArguments.Select((item) => CecilTypeBase.Create(item)).ToArray();
        }
        public CecilGenericMethod(ICecilType DeclaringType, ICecilMethod GenericDeclaration, IEnumerable<IType> TypeArguments)
            : base(DeclaringType)
        {
            this.GenericDeclaration = GenericDeclaration;
            this.TypeArguments = TypeArguments.ToArray();
        }

        public ICecilMethod GenericDeclaration { get; private set; }
        public IType[] TypeArguments { get; private set; }

        public override bool IsComplete
        {
            get
            {
                var cecilTypeArgs = TypeArguments.Select((item) => CecilTypeBase.ImportCecil(item, DeclaringType)).ToArray();
                return cecilTypeArgs.All((item) => item != null && item.IsComplete);
            }
        }

        private MethodReference methodRef;
        public override MethodReference GetMethodReference()
        {
            if (methodRef == null)
            {
                var genericMethodRef = GenericDeclaration.GetMethodReference();
                var module = genericMethodRef.Module;
                var cecilTypeArgs = TypeArguments.Select((item) => item.GetTypeReference(module)).ToArray();
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

        public override IEnumerable<IType> GetCecilGenericArguments()
        {
            return DeclaringType.GetCecilGenericArguments().Concat(TypeArguments);
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
            return GenericDeclaration.GetBaseMethods();
        }

        public override bool IsConstructor
        {
            get { return GenericDeclaration.IsConstructor; }
        }

        public override bool IsStatic
        {
            get { return GenericDeclaration.IsStatic; }
        }

        protected override IType ResolveLocalTypeParameter(IGenericParameter TypeParameter)
        {
            var genericParams = GenericDeclaration.GetGenericParameters().ToArray();
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
            return GenericDeclaration.GetAttributes();
        }

        #endregion

        #region Equality/GetHashCode

        public override bool Equals(ICecilMethod other)
        {
            return GenericDeclaration.Equals(other.GetGenericDeclaration()) && TypeArguments.AreEqual(other.GetGenericArguments().ToArray());
        }

        #endregion
    }
}

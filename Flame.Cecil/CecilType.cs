using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilType : CecilResolvedTypeBase, IDelegateType
    {
        public CecilType(TypeReference Reference, CecilModule Module)
            : base(Module)
        {
            SetReference(Reference);
            delegateSig = new Lazy<IMethod>(GetDelegateSignature);
        }

        private void SetReference(TypeReference Reference)
        {
            this.typeReference = Reference;
            if (Reference is TypeDefinition)
            {
                resolvedType = (TypeDefinition)Reference;
            }
        }

        private TypeReference typeReference;
        private TypeDefinition resolvedType;
        public override TypeDefinition GetResolvedType()
        {
            if (resolvedType == null)
            {
                resolvedType = typeReference.Resolve();
            }
            if (resolvedType == null)
            {
                System.Diagnostics.Debugger.Break();
            }
            return resolvedType;
        }

        public override IEnumerable<IGenericParameter> GetCecilGenericParameters()
        {
            return ConvertGenericParameters(typeReference, typeReference.Resolve, this, Module);
        }

        public override TypeReference GetTypeReference()
        {
            return typeReference;
        }

        public override IAncestryRules AncestryRules
        {
            get { return CecilAncestryRules.Instance; }
        }

        private Lazy<IMethod> delegateSig;
        public IMethod DelegateSignature
        {
            get { return delegateSig.Value; }
        }

        private IMethod GetDelegateSignature()
        {
            if (CecilDelegateType.IsDelegateType(this))
                return GetMethods().Single(item => item.Name.ToString() == "Invoke" && !item.IsStatic);
            else
                return null;
        }
    }
}

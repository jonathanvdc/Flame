using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilType : CecilResolvedTypeBase
    {
        public CecilType(TypeReference Reference)
        {
            SetReference(Reference);
        }
        public CecilType(TypeReference Reference, AncestryGraph Graph)
            : base(Graph)
        {
            SetReference(Reference);
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
            return ConvertGenericParameters(typeReference, typeReference.Resolve, this, AncestryGraph);
        }

        public override TypeReference GetTypeReference()
        {
            return typeReference;
        }
    }
}

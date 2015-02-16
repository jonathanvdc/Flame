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
        private TypeReference genericTypeRef;
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

        /*public override ICecilType GetCecilGenericDeclaration()
        {
            return this;
        }*/

        public override IEnumerable<IGenericParameter> GetCecilGenericParameters()
        {
            return ConvertGenericParameters(typeReference, typeReference.Resolve, this, AncestryGraph);
        }

        public override TypeReference GetTypeReference()
        {
            if (genericTypeRef == null)
            {
                ICecilGenericMember declMember = null;
                if (typeReference.DeclaringType != null)
                {
                    declMember = CecilTypeBase.Create(typeReference.DeclaringType) as ICecilGenericMember;
                }
                if (declMember != null && !this.get_IsGenericParameter())
                {
                    if (declMember.GetCecilGenericParameters().Any())
                    {
                        var cecilTypeArgs = this.GetCecilGenericParameters().Prefer(declMember.GetCecilGenericArguments());
                        var inst = new GenericInstanceType(typeReference);
                        var module = typeReference.Module;
                        foreach (var item in cecilTypeArgs)
                        {
                            inst.GenericArguments.Add(item.GetImportedReference(module, typeReference));
                        }
                        this.genericTypeRef = inst;
                    }
                    else
                    {
                        this.genericTypeRef = typeReference;
                    }
                }
                else
                {
                    this.genericTypeRef = typeReference;
                }
            }
            return genericTypeRef;
        }
    }
}

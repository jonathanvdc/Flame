using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilGenericInstanceType : CecilGenericTypeBase
    {
        public CecilGenericInstanceType(ICecilType DeclaringType, ICecilType GenericDefinition)
            : base(GenericDefinition)
        {
            this.DeclaringType = DeclaringType;
        }

        public ICecilType DeclaringType { get; private set; }        

        public override string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public override string Name
        {
            get { return GenericDefinition.Name; }
        }

        public override TypeReference GetTypeReference()
        {
            var typeRef = GenericDefinition.GetTypeReference();
            var cecilTypeArgs = this.GetAllGenericParameters().Prefer(DeclaringType.GetAllGenericArguments());
            var inst = new GenericInstanceType(typeRef);
            var module = typeRef.Module;
            foreach (var item in cecilTypeArgs)
            {
                inst.GenericArguments.Add(item.GetImportedReference(module, typeRef));
            }
            return inst;
        }

        public override IEnumerable<IType> GetGenericArguments()
        {
            return Enumerable.Empty<IType>();
        }

        public override INamespace DeclaringNamespace
        {
            get { return DeclaringType; }
        }

        public override IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return DeclaringType.ResolveTypeParameter(TypeParameter);
        }

        public override IType GetGenericDeclaration()
        {
            return this;
        }
    }
}

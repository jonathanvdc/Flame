using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilGenericType : CecilGenericTypeBase, IEquatable<ICecilMember>, IEquatable<ICecilType>
    {
        public CecilGenericType(ICecilType GenericDefinition, IEnumerable<IType> TypeArguments)
            : base(GenericDefinition)
        {
            this.TypeArguments = TypeArguments;
        }

        public static TypeReference CreateGenericInstanceReference(TypeReference GenericDeclaration, IEnumerable<TypeReference> GenericArguments)
        {
            var inst = new GenericInstanceType(GenericDeclaration);
            foreach (var item in GenericArguments)
            {
                inst.GenericArguments.Add(item);
            }
            return inst;
        }

        public IEnumerable<IType> TypeArguments { get; private set; }

        private TypeReference typeRef;
        public override TypeReference GetTypeReference()
        {
            if (typeRef == null)
            {
                var declTypeRef = GenericDefinition.GetTypeReference();
                var module = declTypeRef.Module;
                var genericTypeRef = declTypeRef.Resolve();
                var cecilTypeArgs = this.GetAllGenericArguments().Select((item) => item.GetImportedReference(module)).ToArray();
                if (cecilTypeArgs.All((item) => item != null))
                {
                    var inst = new GenericInstanceType(genericTypeRef);
                    foreach (var item in cecilTypeArgs)
                    {
                        inst.GenericArguments.Add(item);
                    }
                    typeRef = inst;
                }
                else
                {
                    typeRef = genericTypeRef; // Kind of sad, really. Oh, well.
                }
            }
            return typeRef;
        }

        public override INamespace DeclaringNamespace
        {
            get
            {
                return GenericDefinition.GetTypeReference().GetDeclaringNamespace();
            }
        }

        #region IMember Implementation

        private string nameCache;
        public override string Name
        {
            get
            {
                if (nameCache == null)
                {
                    nameCache = GenericNameExtensions.ChangeTypeArguments(GenericDefinition.Name, TypeArguments.Select(item => item.Name));
                }
                return nameCache;
            }
        }

        private string fullNameCache;
        public override string FullName
        {
            get
            {
                if (fullNameCache == null)
                {
                    fullNameCache = GenericNameExtensions.ChangeTypeArguments(GenericDefinition.FullName, TypeArguments.Select(item => item.FullName));
                }
                return fullNameCache;
            }
        }

        #endregion

        #region Generics

        public override IEnumerable<IType> GetGenericArguments()
        {
            return TypeArguments;
        }

        public override IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            var allTypeParams = this.GetAllGenericParameters();
            var allTypeArgs = this.GetAllGenericArguments();
            var genericParams = allTypeParams.ToArray();
            for (int i = 0; i < genericParams.Length; i++)
            {
                if (genericParams[i].Equals(TypeParameter))
                {
                    return allTypeArgs.ElementAt(i);
                }
            }
            string name = TypeParameter.Name;
            if (name.StartsWith("!"))
            {
                int index = int.Parse(name.Substring(1));
                return allTypeArgs.ElementAt(index);
            }
            return null;
        }

        public override IType GetGenericDeclaration()
        {
            if (GenericDefinition.GetAllGenericArguments().Any())
            {
                return new CecilGenericInstanceType((ICecilType)GenericDefinition.DeclaringNamespace, GenericDefinition);
            }
            else
            {
                return GenericDefinition;
            }
        }

        #endregion
    }
}

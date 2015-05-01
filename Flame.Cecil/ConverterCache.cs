using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class ConverterCache
    {
        public ConverterCache()
        {
            this.ConvertedTypes = new Dictionary<TypeReference, IType>();
            this.ConvertedStrictTypes = new Dictionary<TypeReference, IType>();
            this.ConvertedMethods = new Dictionary<MethodReference, ICecilMethod>();
            this.ConvertedFields = new Dictionary<FieldReference, ICecilField>();
        }

        public Dictionary<TypeReference, IType> ConvertedTypes { get; private set; }
        public Dictionary<TypeReference, IType> ConvertedStrictTypes { get; private set; }
        public Dictionary<MethodReference, ICecilMethod> ConvertedMethods { get; private set; }
        public Dictionary<FieldReference, ICecilField> ConvertedFields { get; private set; }
    }
}

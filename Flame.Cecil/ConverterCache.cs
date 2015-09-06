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
            this.ConvertedMethods = new Dictionary<MethodReference, IMethod>();
            this.ConvertedFields = new Dictionary<FieldReference, IField>();
        }

        public Dictionary<TypeReference, IType> ConvertedTypes { get; private set; }
        public Dictionary<TypeReference, IType> ConvertedStrictTypes { get; private set; }
        public Dictionary<MethodReference, IMethod> ConvertedMethods { get; private set; }
        public Dictionary<FieldReference, IField> ConvertedFields { get; private set; }
    }
}

using Flame.Compiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilEnumBuilder : CecilTypeBuilder
    {
        public CecilEnumBuilder(TypeDefinition Definition, INamespace DeclaringNamespace)
            : base(Definition, DeclaringNamespace)
        {
        }
        public CecilEnumBuilder(CecilResolvedTypeBase Type, INamespace DeclaringNamespace)
            : base(Type, DeclaringNamespace)
        {
        }

        public override IFieldBuilder DeclareField(IField Template)
        {
            return CecilField.DeclareEnumField(this, Template);
        }
    }
}

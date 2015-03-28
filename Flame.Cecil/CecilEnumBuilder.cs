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
        public CecilEnumBuilder(TypeDefinition Definition, INamespace DeclaringNamespace, CecilModule Module)
            : base(Definition, DeclaringNamespace, Module)
        {
        }
        public CecilEnumBuilder(CecilResolvedTypeBase Type, INamespace DeclaringNamespace, CecilModule Module)
            : base(Type, DeclaringNamespace, Module)
        {
        }

        public override IFieldBuilder DeclareField(IField Template)
        {
            return CecilField.DeclareEnumField(this, Template);
        }
    }
}

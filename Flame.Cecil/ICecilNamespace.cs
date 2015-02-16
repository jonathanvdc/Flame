using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public interface ICecilNamespace : INamespace
    {
        ModuleDefinition GetModule();
        void AddType(TypeDefinition Definition);
    }
}

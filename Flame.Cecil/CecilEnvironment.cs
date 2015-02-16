using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilEnvironment : IEnvironment
    {
        public CecilEnvironment(ModuleDefinition Module)
        {
            this.Module = Module;
        }

        public ModuleDefinition Module { get; private set; }

        public string Name
        {
            get { return "CLR/Cecil"; }
        }

        public IType RootType
        {
            get { return CecilTypeBase.CreateCecil(Module.TypeSystem.Object); }
        }

        public IType EnumerableType
        {
            get { return CecilTypeBase.ImportCecil(typeof(IEnumerable<>), Module); }
        }

        public IType EnumeratorType
        {
            get { return CecilTypeBase.ImportCecil(typeof(IEnumerator<>), Module); }
        }
    }
}

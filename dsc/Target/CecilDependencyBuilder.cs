using Flame;
using Flame.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Target
{
    public class CecilDependencyBuilder : DependencyBuilder
    {
        public CecilDependencyBuilder(IAssemblyResolver RuntimeLibaryResolver, IEnvironment Environment, string CurrentPath, string OutputFolder, Flame.Cecil.SpecificAssemblyResolver CecilResolver)
            : base(RuntimeLibaryResolver, Environment, CurrentPath, OutputFolder)
        {
            this.CecilResolver = CecilResolver;
        }

        public Flame.Cecil.SpecificAssemblyResolver CecilResolver { get; private set; }

        protected override void RegisterAssembly(IAssembly Assembly)
        {
            base.RegisterAssembly(Assembly);
            if (Assembly is CecilAssembly)
            {
                CecilResolver.AddAssembly(((CecilAssembly)Assembly).Assembly);
            }
        }
    }
}

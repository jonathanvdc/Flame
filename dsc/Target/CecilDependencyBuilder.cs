using Flame;
using Flame.Cecil;
using Flame.Front;
using Flame.Front.Plugs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Target
{
    public static class CecilDependencyBuilderExtensions
    {
        private const string CecilAssemblyResolverKey = "CecilAssemblyResolver";

        private static bool HasCecilResolver(this IDependencyBuilder DependencyBuilder)
        {
            return DependencyBuilder.Properties.ContainsKey(CecilAssemblyResolverKey);
        }

        public static Mono.Cecil.IAssemblyResolver GetCecilResolver(this IDependencyBuilder DependencyBuilder)
        {
            if (DependencyBuilder == null)
            {
                var resolver = new Mono.Cecil.DefaultAssemblyResolver();
                resolver.AddSearchDirectory(FlameAssemblies.FlameAssemblyDirectory.Path);
                return resolver;
            }
            if (!HasCecilResolver(DependencyBuilder))
            {
                DependencyBuilder.SetupCecil();
            }
            return DependencyBuilder.Properties.GetValue<Mono.Cecil.IAssemblyResolver>(CecilAssemblyResolverKey);
        }

        public static void SetCecilResolver(this IDependencyBuilder DependencyBuilder, Mono.Cecil.IAssemblyResolver Resolver)
        {
            DependencyBuilder.AddCecilAssemblyRegisteredCallback();
            DependencyBuilder.Properties.SetValue(CecilAssemblyResolverKey, Resolver);
        }

        private static void SetupCecil(this IDependencyBuilder DependencyBuilder)
        {
            if (!DependencyBuilder.HasCecilResolver())
            {
                DependencyBuilder.SetCecilResolver(new Flame.Cecil.SpecificAssemblyResolver());
            }
        }

        private static void AddCecilAssemblyRegisteredCallback(this IDependencyBuilder DependencyBuilder)
        {
            DependencyBuilder.AddAssemblyRegisteredCallback("Cecil", (asm) =>
            {
                if (asm is CecilAssembly)
                {
                    var resolver = DependencyBuilder.GetCecilResolver();
                    if (resolver is Flame.Cecil.SpecificAssemblyResolver)
                    {
                        ((Flame.Cecil.SpecificAssemblyResolver)resolver).AddAssembly(((CecilAssembly)asm).Assembly);
                    }
                }
            });
        }
    }
}

using Flame;
using Flame.Cecil;
using Flame.Front;
using Flame.Front.Plugs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
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
            DependencyBuilder.SetupCecil();
            return DependencyBuilder.Properties.GetValue<Mono.Cecil.IAssemblyResolver>(CecilAssemblyResolverKey);
        }

        public static Mono.Cecil.ReaderParameters GetCecilReaderParameters(this IDependencyBuilder DependencyBuilder)
        {
            var resolver = DependencyBuilder.GetCecilResolver();
            if (resolver is SpecificAssemblyResolver)
            {
                return ((SpecificAssemblyResolver)resolver).ReaderParameters;
            }
            else
            {
                var readerParams = new Mono.Cecil.ReaderParameters();
                readerParams.AssemblyResolver = resolver;
                return readerParams;
            }
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
                if (DependencyBuilder.Environment is CecilEnvironment)
                {
                    var env = (CecilEnvironment)DependencyBuilder.Environment;
                    DependencyBuilder.SetCecilResolver(env.Module.Module.AssemblyResolver);
                }
                else
                {
                    DependencyBuilder.SetCecilResolver(ClrBuildTargetParser.CreateCecilAssemblyResolver());
                }
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

using Flame.Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public static class CppDependencyBuilderExtensions
    {
        private const string CppAssemblyResolverKey = "C++/Environment";

        private static bool HasCppEnvironment(this IDependencyBuilder DependencyBuilder)
        {
            return DependencyBuilder.Properties.ContainsKey(CppAssemblyResolverKey);
        }

        public static ICppEnvironment GetCppEnvironment(this IDependencyBuilder DependencyBuilder)
        {
            if (!HasCppEnvironment(DependencyBuilder))
            {
                DependencyBuilder.SetCppEnvironment(CppEnvironment.Create(DependencyBuilder.Log));
            }
            return DependencyBuilder.Properties.GetValue<ICppEnvironment>(CppAssemblyResolverKey);
        }

        public static void SetCppEnvironment(this IDependencyBuilder DependencyBuilder, ICppEnvironment Environment)
        {
            DependencyBuilder.Properties.SetValue(CppAssemblyResolverKey, Environment);
        }
    }
}

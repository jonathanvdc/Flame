using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public static class VerificationExtensions
    {
        public static bool VerifyAttribute<T>(this IAttribute Attribute, T Member, IEnumerable<IAttributeVerifier<T>> Verifiers, ICompilerLog Log)
            where T : IMember
        {
            bool success = true;
            foreach (var item in Verifiers)
            {
                if (!item.Verify(Attribute, Member, Log))
                {
                    success = false;
                }
            }
            return success;
        }

        public static bool VerifyImplementation(this IMethod DefinitionMethod, IType ImplementationType, ICompilerLog Log)
        {
            bool success = true;
            if (DefinitionMethod.get_IsAbstract() || DefinitionMethod.DeclaringType.get_IsInterface())
            {
                var impl = DefinitionMethod.GetImplementation(ImplementationType);
                if (impl == null || impl.Equals(DefinitionMethod))
                {
                    Log.LogError(new LogEntry("Method not implemented", 
                        "Method '" + DefinitionMethod.FullName + "' was not implemented in '" + ImplementationType.FullName + "'",
                        ImplementationType.GetSourceLocation()));
                    success = false;
                }
            }
            return success;
        }

        public static bool VerifyImplementation(this IType DefinitionType, IType ImplementationType, ICompilerLog Log)
        {
            bool success = true;
            foreach (var item in DefinitionType.GetAllMethods())
            {
                if (!item.VerifyImplementation(ImplementationType, Log)) success = false;
            }
            foreach (var prop in DefinitionType.GetAllProperties())
            foreach (var item in prop.GetAccessors())
            {
                if (!item.VerifyImplementation(ImplementationType, Log)) success = false;
            }
            return success;
        }

        /// <summary>
        /// Provides easy full assembly bare-bones verification.
        /// </summary>
        /// <param name="Assembly"></param>
        /// <param name="Log"></param>
        /// <returns></returns>
        public static bool VerifyAssembly(this IAssembly Assembly, ICompilerLog Log)
        {
            var verifier = new AssemblyVerifier();
            return verifier.Verify(Assembly, Log);
        }
    }
}

using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class CecilCompilerOptionExtensions
    {
        /// <summary>
        /// Gets a boolean value that indicates whether the given compiler options specify that an invariant culture should be used on Parse/ToString operations when no culture is explicitly specified.
        /// </summary>
        /// <param name="Options"></param>
        /// <returns></returns>
        public static bool UseInvariantCulture(this ICompilerOptions Options)
        {
            return Options.GetOption<bool>("invariant-culture", false);
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the given compiler options specify static methods should be generated for instance singleton methods.
        /// </summary>
        /// <param name="Options"></param>
        /// <returns></returns>
        public static bool GenerateStaticMembers(this ICompilerOptions Options)
        {
            return Options.GetOption<bool>("generate-static", false);
        }

        public static ICompilerLog CreateEmptyLog()
        {
            return new EmptyCompilerLog(new EmptyCompilerOptions());
        }

        public static ICompilerLog GetLog(this IAssembly Assembly)
        {
            if (Assembly is ILogAssembly)
            {
                return ((ILogAssembly)Assembly).Log;
            }
            else
            {
                return CreateEmptyLog();
            }
        }

        public static ICompilerLog GetLog(this INamespace Namespace)
        {
            return Namespace.DeclaringAssembly == null ? CreateEmptyLog() : Namespace.DeclaringAssembly.GetLog();
        }

        public static ICompilerLog GetLog(this IType Type)
        {
            return Type.DeclaringNamespace == null ? CreateEmptyLog() : Type.DeclaringNamespace.GetLog();
        }

        public static ICompilerLog GetLog(this ITypeMember Member)
        {
            return Member.DeclaringType == null ? CreateEmptyLog() : Member.DeclaringType.GetLog();
        }
    }
}

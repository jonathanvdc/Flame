using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Syntax;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public static class VerificationExtensions
    {
        public static string GetAccessModifierName(this AccessModifier Modifier)
        {
            switch (Modifier)
            {
                case AccessModifier.Assembly:
                    return "assembly";
                case AccessModifier.Private:
                    return "private";
                case AccessModifier.Protected:
                    return "protected";
                case AccessModifier.ProtectedAndAssembly:
                    return "protected and assembly";
                case AccessModifier.ProtectedOrAssembly:
                    return "protected or assembly";
                case AccessModifier.Public:
                default:
                    return "public";
            }
        }

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

        private static IMarkupNode CreateImplementationDiagnostics(IMethod DefinitionMethod, IType ImplementationType)
        {
            var implLoc = ImplementationType.GetSourceLocation();
            var defLoc = DefinitionMethod.GetSourceLocation();

            var mainNode = implLoc.CreateDiagnosticsNode();

            if (defLoc == null)
            {
                return implLoc.CreateDiagnosticsNode();
            }
            else if (implLoc == null)
            {
                return RedefinitionHelpers.Instance.CreateNeutralDiagnosticsNode("Definition: ", defLoc);
            }
            else
            {
                return RedefinitionHelpers.Instance.AppendDiagnosticsRemark(mainNode, "Definition: ", defLoc);
            }
        }

        private static IMarkupNode CreateImplementationNode(IMethod DefinitionMethod, IType ImplementationType)
        {
            var message = new MarkupNode(NodeConstants.TextNodeType, "Method '" + DefinitionMethod.FullName + "' was not implemented in '" + ImplementationType.FullName + "'.");
            var diagnostics = CreateImplementationDiagnostics(DefinitionMethod, ImplementationType);
            return new MarkupNode("entry", new[] { message, diagnostics });
        }

        public static bool VerifyImplementation(this IMethod DefinitionMethod, IType ImplementationType, ICompilerLog Log)
        {
            bool success = true;
            if (DefinitionMethod.GetIsAbstract() || DefinitionMethod.DeclaringType.GetIsInterface())
            {
                var impl = DefinitionMethod.GetImplementation(ImplementationType);
                if (impl == null || impl.Equals(DefinitionMethod))
                {
                    Log.LogError(new LogEntry("Method not implemented", CreateImplementationNode(DefinitionMethod, ImplementationType)));
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
            foreach (var item in prop.Accessors)
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

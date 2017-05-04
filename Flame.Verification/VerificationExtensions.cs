using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Syntax;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Build;

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

        private static MarkupNode CreateSyntheticSignatureNode(
            IMethod Method, Func<IMethod, string> DescribeMethodSignature)
        {
            return new MarkupNode(NodeConstants.SourceNodeType, new[] 
            {
                new MarkupNode(NodeConstants.TextNodeType, DescribeMethodSignature(Method))
            });
        }

        private static MarkupNode CreateDefinitionDiagnostics(
            IMethod Method, Func<IMethod, string> DescribeMethodSignature)
        {
            var defLoc = Method.GetSourceLocation();

            if (defLoc != null)
            {
                return defLoc.CreateRemarkDiagnosticsNode("definition: ");
            }
            else
            {
                var src = CreateSyntheticSignatureNode(Method, DescribeMethodSignature);
                var neutralSrc = new MarkupNode("neutral-diagnostics", new[] { src });
                var title = new MarkupNode(NodeConstants.BrightNodeType, "signature: ");

                return new MarkupNode(NodeConstants.RemarksNodeType, new[]
                {
                    title,
                    neutralSrc
                });
            }
        }

        private static MarkupNode CreateImplementationDiagnostics(
            IMethod DefinitionMethod, IType ImplementationType, 
            Func<IMethod, string> DescribeMethodSignature)
        {
            var implLoc = ImplementationType.GetSourceLocation();
            var defNode = CreateDefinitionDiagnostics(DefinitionMethod, DescribeMethodSignature);

            if (implLoc == null)
            {
                return defNode;
            }
            else
            {
                var mainNode = implLoc.CreateDiagnosticsNode();
                return new MarkupNode("entry", new[] { mainNode, defNode });
            }
        }

        private static MarkupNode CreateImplementationNode(
            IMethod DefinitionMethod, IType ImplementationType,
            Func<IMethod, string> DescribeMethodSignature)
        {
            var renderer = new TypeRenderer()
                .AbbreviateTypeNames(SimpleTypeFinder.Instance.ConvertAndMerge(
                    new IType[] { DefinitionMethod.DeclaringType, ImplementationType }));
            var message = new MarkupNode(
                NodeConstants.TextNodeType,
                "method '" + renderer.MakeNestedType(
                    renderer.Convert(DefinitionMethod.DeclaringType),
                    renderer.CreateTextNode(renderer.UnqualifiedNameToString(DefinitionMethod.Name)),
                    renderer.DefaultStyle).GetAllText() +
                "' was not implemented in '" + renderer.Name(ImplementationType) + "'.");
            var diagnostics = CreateImplementationDiagnostics(DefinitionMethod, ImplementationType, DescribeMethodSignature);
            return new MarkupNode("entry", new[] { message, diagnostics });
        }

        public static bool VerifyImplementation(
            this IMethod DefinitionMethod, IType ImplementationType, 
            ICompilerLog Log, Func<IMethod, string> DescribeMethodSignature)
        {
            bool success = true;
            if (DefinitionMethod.GetIsAbstract() || DefinitionMethod.DeclaringType.GetIsInterface())
            {
                var impl = DefinitionMethod.GetImplementation(ImplementationType);
                if (impl == null || impl.Equals(DefinitionMethod))
                {
                    Log.LogError(new LogEntry(
                        "method not implemented", 
                        CreateImplementationNode(
                            DefinitionMethod, 
                            ImplementationType, 
                            DescribeMethodSignature)));
                    success = false;
                }
            }
            return success;
        }

        public static bool VerifyImplementation(
            this IType DefinitionType, IType ImplementationType, 
            ICompilerLog Log, 
            Func<IMethod, string> DescribeMethodSignature)
        {
            bool success = true;
            foreach (var item in DefinitionType.GetAllMethods())
            {
                if (!item.VerifyImplementation(ImplementationType, Log, DescribeMethodSignature)) 
                    success = false;
            }
            foreach (var prop in DefinitionType.GetAllProperties())
            foreach (var item in prop.Accessors)
            {
                if (!item.VerifyImplementation(ImplementationType, Log, DescribeMethodSignature)) 
                    success = false;
            }
            return success;
        }

        /// <summary>
        /// The default method attribute description implementation:
        /// the given method's relevant attributes are described.
        /// </summary>
        public static string DescribeAttributesDefault(ITypeMember Member)
        {
            var sb = new StringBuilder();
            sb.Append(GetAccessModifierName(Member.GetAccess()));
            if (Member is IMethod)
            {
                var method = (IMethod)Member;
                if (method.GetIsAbstract())
                {
                    sb.Append(" abstract");
                }
                if (method.BaseMethods.Any(item => !item.DeclaringType.GetIsInterface()))
                {
                    sb.Append(" override");
                }
            }
            if (Member.GetIsConstant())
            {
                sb.Append(" const");
            }
            return sb.ToString();
        }

        /// <summary>
        /// The default parameter description implementation.
        /// </summary>
        public static string DescribeParameterDefault(
            IParameter Parameter, Func<IType, string> DescribeType)
        {
            return DescribeType(Parameter.ParameterType) + " " + Parameter.Name;
        }

        /// <summary>
        /// The default parameter list description implementation.
        /// </summary>
        public static string DescribeParameterListDefault(
            IEnumerable<IParameter> Parameters,
            string LeftDelimiter, string RightDelimiter,
            Func<IParameter, string> DescribeParameter)
        {
            var sb = new StringBuilder();
            sb.Append(LeftDelimiter);
            sb.Append(string.Join(", ", Parameters.Select(DescribeParameter)));
            sb.Append(RightDelimiter);
            return sb.ToString();
        }

        /// <summary>
        /// The default parameter list description implementation.
        /// </summary>
        public static string DescribeParameterListDefault(
            IEnumerable<IParameter> Parameters,
            string LeftDelimiter, string RightDelimiter,
            TypeRenderer Renderer)
        {
            return DescribeParameterListDefault(
                Parameters, LeftDelimiter, RightDelimiter,
                item => DescribeParameterDefault(item, Renderer.Name));
        }

        /// <summary>
        /// Generates a string that describes the given method's signature.
        /// </summary>
        /// <returns>A method signature string.</returns>
        /// <param name="Method">The method to describe.</param>
        public static string DescribeMethodDefault(IMethod Method)
        {
            var typeRenderer = new TypeRenderer()
                .AbbreviateTypeNames(SimpleTypeFinder.Instance.Convert(Method));

            var sb = new StringBuilder();
            if (Method is IAccessor)
            {
                var acc = (IAccessor)Method;
                sb.Append(DescribeAttributesDefault(acc.DeclaringProperty));
                sb.Append(' ');
                sb.Append(typeRenderer.Name(acc.DeclaringProperty.PropertyType));
                sb.Append(' ');
                sb.Append(acc.DeclaringProperty.Name);
                var parameters = Method.GetParameters();
                if (parameters.Length > 0)
                {
                    sb.Append(DescribeParameterListDefault(parameters, "[", "]", typeRenderer));
                }
                sb.Append(" { ");
                sb.Append(DescribeAttributesDefault(acc));
                sb.Append(' ');
                sb.Append(acc.AccessorType.Name.ToLower());
                sb.Append("; }");
            }
            else
            {
                sb.Append(DescribeAttributesDefault(Method));
                sb.Append(' ');
                sb.Append(typeRenderer.Name(Method.ReturnType));
                sb.Append(' ');
                sb.Append(Method.Name);
                sb.Append(DescribeParameterListDefault(Method.Parameters, "(", ");", typeRenderer));
            }
            return sb.ToString();
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

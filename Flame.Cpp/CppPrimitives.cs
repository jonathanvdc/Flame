using Flame.Build;
using Flame.Compiler;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public static class CppPrimitives
    {
        private static IMethod createSharedPtr;
        public static IMethod CreateSharedPointer
        {
            get
            {
                if (createSharedPtr == null)
                {
                    // C++ signature: 
                    // template<typename T>
                    // std::shared_ptr<T> std::shared_ptr::shared_ptr(T* Instance)
                    // Flame signature (D#):
                    // T^ std.shared_ptr<T>(T* Instance)
                    var descMethod = new DescribedMethod("std.shared_ptr<>", null);
                    descMethod.IsStatic = true;
                    var genParam = new DescribedGenericParameter("T", descMethod);
                    descMethod.AddGenericParameter(genParam);
                    descMethod.AddParameter(new DescribedParameter("Instance", genParam.MakePointerType(PointerKind.TransientPointer)));
                    descMethod.ReturnType = genParam.MakePointerType(PointerKind.ReferencePointer);
                    createSharedPtr = descMethod;
                }
                return createSharedPtr;
            }
        }

        public static IMethod GetMakeSharedPointerMethod(IType ElementType, IEnumerable<IType> ArgumentTypes)
        {
            // C++ signature:
            // template<typename T>
            // std::shared_ptr<T> std::make_shared(TArgs... Args)
            // Flame signature (D#)
            // T^ std.make_shared<T>(TArg0 Arg0, TArg1 Arg1, ...)

            var descMethod = new DescribedMethod("std.make_shared<>", null);
            descMethod.IsStatic = true;
            var genParam = new DescribedGenericParameter("T", descMethod);
            descMethod.AddGenericParameter(genParam);
            descMethod.ReturnType = genParam;
            int i = 0;
            foreach (var item in ArgumentTypes)
            {
                descMethod.AddParameter(new DescribedParameter("Arg" + i, item));
                i++;
            }
            return descMethod.MakeGenericMethod(new IType[] { ElementType });
        }

        private static IMethod toStrMethod;
        public static IMethod ToStringMethod
        {
            get
            {
                if (toStrMethod == null)
                {
                    // C++ signature: (actually a bunch of overrides, but nevermind that)
                    // template<typename T>
                    // std::string std::to_string(T Value)
                    // Flame signature (D#)
                    // string std.to_string<T>(T Value)

                    var descMethod = new DescribedMethod("std.to_string<>", null);
                    descMethod.IsStatic = true;
                    var genParam = new DescribedGenericParameter("T", descMethod);
                    descMethod.AddGenericParameter(genParam);
                    descMethod.ReturnType = PrimitiveTypes.String;
                    descMethod.AddParameter(new DescribedParameter("Value", genParam));
                    toStrMethod = descMethod;
                }
                return toStrMethod;
            }
        }

        public static ICppBlock GetToStringMethodBlock(IType SourceType, ICodeGenerator CodeGenerator)
        {
            var inst = ToStringMethod.MakeGenericMethod(new IType[] { SourceType });
            return new RetypedBlock(ToStringMethod.CreateBlock(CodeGenerator), MethodType.Create(inst));
        }

        private static IMethod assertMethod;
        public static IMethod AssertMethod
        {
            get
            {
                // assert(...) is a macro in C++,
                // but that distinction is not entirely relevant from a code generation viewpoint.

                if (assertMethod == null)
                {
                    var descMethod = new DescribedMethod("assert", null, PrimitiveTypes.Void, true);
                    descMethod.AddParameter(new DescribedParameter("Condition", PrimitiveTypes.Boolean));
                    descMethod.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
                    descMethod.AddAttribute(new HeaderDependencyAttribute(StandardDependency.CAssert));
                    assertMethod = descMethod;
                }
                return assertMethod;
            }
        }

        private static IMethod requireMethod;
        public static IMethod RequireMethod
        {
            get
            {
                // require(...) is a macro in C++,
                // but that distinction is not entirely relevant from a code generation viewpoint

                if (requireMethod == null)
                {
                    var descMethod = new DescribedMethod("require", null, PrimitiveTypes.Void, true);
                    descMethod.AddParameter(new DescribedParameter("Condition", PrimitiveTypes.Boolean));
                    descMethod.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
                    descMethod.AddAttribute(new HeaderDependencyAttribute(Plugs.ContractsHeader.Instance));
                    requireMethod = descMethod;
                }
                return requireMethod;
            }
        }

        private static IMethod ensureMethod;
        public static IMethod EnsureMethod
        {
            get
            {
                // require(...) is a macro in C++,
                // but that distinction is not entirely relevant from a code generation viewpoint

                if (ensureMethod == null)
                {
                    var descMethod = new DescribedMethod("ensure", null, PrimitiveTypes.Void, true);
                    descMethod.AddParameter(new DescribedParameter("Condition", PrimitiveTypes.Boolean));
                    descMethod.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
                    descMethod.AddAttribute(new HeaderDependencyAttribute(Plugs.ContractsHeader.Instance));
                    ensureMethod = descMethod;
                }
                return ensureMethod;
            }
        }

        private static IDictionary<PointerKind, IMethod> isInstMethods;
        public static IMethod GetIsInstanceMethod(PointerKind Kind)
        {
            // C++ signature (for std::shared_ptr<TSource>):
            //
            // template<typename TTarget, typename TSource>
            // inline bool isinstance(std::shared_ptr<TSource> Value);
            //
            // Other pointer types are analogous

            if (isInstMethods == null)
            {
                isInstMethods = new Dictionary<PointerKind, IMethod>();
            }
            if (!isInstMethods.ContainsKey(Kind))
            {
                var descMethod = new DescribedMethod("stdx.isinstance<,>", null, PrimitiveTypes.Boolean, true);
                descMethod.AddGenericParameter(new DescribedGenericParameter("TTarget", descMethod));
                var tSource = new DescribedGenericParameter("TSource", descMethod);
                descMethod.AddGenericParameter(tSource);
                descMethod.AddParameter(new DescribedParameter("Pointer", tSource.MakePointerType(Kind)));
                descMethod.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
                descMethod.AddAttribute(new HeaderDependencyAttribute(Plugs.IsInstanceHeader.Instance));
                isInstMethods[Kind] = descMethod;
            }
            return isInstMethods[Kind];
        }
        public static IMethod GetIsInstanceMethod(PointerKind Kind, IType SourceType, IType TargetType)
        {
            return GetIsInstanceMethod(Kind).MakeGenericMethod(new IType[] { TargetType, SourceType });
        }
    }
}

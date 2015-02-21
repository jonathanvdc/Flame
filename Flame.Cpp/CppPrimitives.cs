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
    }
}

using Flame.Build;
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
    }
}

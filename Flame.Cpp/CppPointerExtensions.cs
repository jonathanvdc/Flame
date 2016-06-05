using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public static class CppPointerExtensions
    {
        public static PointerKind AtAddressPointer
        {
            get
            {
                return PointerKind.Register("&");
            }
        }

        public static bool IsAtAddressPointer(this IType Type)
        {
            return Type.GetIsPointer() && Type.AsContainerType().AsPointerType().PointerKind.Equals(AtAddressPointer);
        }

        public static IType RemoveAtAddressPointers(this IType Type)
        {
            if (Type.IsAtAddressPointer())
            {
                return Type.AsContainerType().ElementType.RemoveAtAddressPointers();
            }
            else
            {
                return Type;
            }
        }

        public static bool IsPrimitivePointer(this PointerType Type)
        {
            return Type.PointerKind.Equals(PointerKind.TransientPointer) || Type.PointerKind.Equals(AtAddressPointer);
        }
    }
}

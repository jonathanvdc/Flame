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

        public static IType RemoveAtAddressPointers(this IType Type)
        {
            if (Type.get_IsPointer() && Type.AsContainerType().AsPointerType().PointerKind.Equals(AtAddressPointer))
            {
                return Type.AsContainerType().GetElementType().RemoveAtAddressPointers();
            }
            else
            {
                return Type;
            }
        }
    }
}

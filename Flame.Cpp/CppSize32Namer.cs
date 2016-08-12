using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppSize32Namer : CppTypeNamerBase
    {
        public CppSize32Namer(INamespace CurrentNamespace)
            : base(CurrentNamespace)
        {
        }

        public override string NameInt(int PrimitiveBitSize)
        {
            switch (PrimitiveBitSize)
            {
                case 8:
                    return "char";
                case 16:
                    return "short";
                case 32:
                    return "int";
                case 64:
                    return "long long";
                default:
                    throw new NotImplementedException();
            }
        }

        public override string NameUInt(int PrimitiveBitSize)
        {
            return "unsigned " + NameInt(PrimitiveBitSize);
        }

        public override string NameFloat(int PrimitiveBitSize)
        {
            switch (PrimitiveBitSize)
            {
                case 32:
                    return "float";
                case 64:
                    return "double";
                case 128:
                    return "long double";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

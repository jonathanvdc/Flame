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

        public override string NameInt(int PrimitiveMagnitude)
        {
            switch (PrimitiveMagnitude)
            {
                case 1:
                    return "char";
                case 2:
                    return "short";
                case 3:
                    return "int";
                case 4:
                    return "long long";
                default:
                    throw new NotImplementedException();
            }
        }

        public override string NameUInt(int PrimitiveMagnitude)
        {
            return "unsigned " + NameInt(PrimitiveMagnitude);
        }

        public override string NameFloat(int PrimitiveMagnitude)
        {
            switch (PrimitiveMagnitude)
            {
                case 3:
                    return "float";
                case 4:
                    return "double";
                case 5:
                    return "long double";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

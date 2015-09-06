using Flame.Build;
using Flame.Python.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonConsoleType : PythonPrimitiveType
    {
        static PythonConsoleType()
        {
            Instance = new PythonConsoleType();

            var wlnStrMethod = new DescribedMethod("WriteLine", Instance, PrimitiveTypes.Void, true);
            wlnStrMethod.AddParameter(new DescribedParameter("Value", PrimitiveTypes.String));
            writelnStringMethod = wlnStrMethod;
            readlnMethod = new DescribedMethod("ReadLine", Instance, PrimitiveTypes.String, true);

            PythonPrimitiveMap.MapPrimitiveMethod(writelnStringMethod, (cg, target) => new PythonIdentifierBlock(cg, "print", MethodType.Create(writelnStringMethod)));
            PythonPrimitiveMap.MapPrimitiveMethod(readlnMethod, (cg, target) => new PythonIdentifierBlock(cg, "input", MethodType.Create(readlnMethod)));
        }

        private PythonConsoleType()
        {
        }

        private static IMethod writelnStringMethod;
        private static IMethod readlnMethod;

        public static PythonConsoleType Instance { get; private set; }

        public override IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[] { PrimitiveAttributes.Instance.StaticTypeAttribute }; }
        }

        public override string Name
        {
            get { return "Console"; }
        }

        public override IEnumerable<IMethod> Methods
        {
            get
            {
                return Enumerable.Concat(base.Methods, new IMethod[]
                {
                    writelnStringMethod,
                    readlnMethod
                });
            }
        }
    }
}

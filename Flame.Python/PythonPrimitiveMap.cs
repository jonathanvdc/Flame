using Flame.Compiler;
using Flame.Python.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public static class PythonPrimitiveMap
    {
        static PythonPrimitiveMap()
        {
            mappedPrimMethods = new Dictionary<IMethod, Func<ICodeGenerator, IPythonBlock, IPythonBlock>>();

            #region Strings

            var endsWithMethod = PrimitiveTypes.String.GetMethod(new SimpleName("EndsWith"), false, PrimitiveTypes.Boolean, new IType[] { PrimitiveTypes.String });
            MapPrimitiveMethod(endsWithMethod, (cg, caller) => new MemberAccessBlock(cg, caller, "endswith", MethodType.Create(endsWithMethod)));
            var startsWithMethod = PrimitiveTypes.String.GetMethod(new SimpleName("StartsWith"), false, PrimitiveTypes.Boolean, new IType[] { PrimitiveTypes.String });
            MapPrimitiveMethod(startsWithMethod, (cg, caller) => new MemberAccessBlock(cg, caller, "startswith", MethodType.Create(startsWithMethod)));
            var concatMethod = PrimitiveTypes.String.GetMethod(new SimpleName("Concat"), true, PrimitiveTypes.String, new IType[] { PrimitiveTypes.String, PrimitiveTypes.String });
            MapPrimitiveMethod(concatMethod, (cg, caller) => new PartialBinaryOperation(cg, PrimitiveTypes.String, Operator.Add));
            var nullOrEmptyMethod = PrimitiveTypes.String.GetMethod(new SimpleName("IsNullOrEmpty"), true, PrimitiveTypes.Boolean, new IType[] { PrimitiveTypes.String });
            MapPrimitiveMethod(nullOrEmptyMethod, (cg, caller) =>
                new PartialRedirectedBinaryOperation(cg,
                    new PartialRedirectedBinaryOperation(cg, new PartialArgumentBlock(cg, PrimitiveTypes.String, 0), Operator.CheckEquality, (IPythonBlock)cg.EmitNull()),
                    Operator.LogicalOr,
                    new PartialRedirectedBinaryOperation(cg, new PartialInvocationBlock(cg, new PythonIdentifierBlock(cg, "len", PythonObjectType.Instance), PrimitiveTypes.Int32), Operator.CheckGreaterThan, (IPythonBlock)cg.EmitInteger(new IntegerValue(0)))
                ));
            var nullOrWhitespaceMethod = PrimitiveTypes.String.GetMethod(new SimpleName("IsNullOrWhiteSpace"), true, PrimitiveTypes.Boolean, new IType[] { PrimitiveTypes.String });
            MapPrimitiveMethod(nullOrWhitespaceMethod, (cg, caller) =>
                new PartialRedirectedBinaryOperation(cg,
                    new PartialRedirectedBinaryOperation(cg, new PartialArgumentBlock(cg, PrimitiveTypes.String, 0), Operator.CheckEquality, (IPythonBlock)cg.EmitNull()),
                    Operator.LogicalOr,
                    new PartialMemberAccessBlock(cg, new PartialArgumentBlock(cg, PrimitiveTypes.String, 0), new PythonIdentifierBlock(cg, "isspace()", PrimitiveTypes.Boolean))
                ));
            var substringMethod = PrimitiveTypes.String.GetMethod(new SimpleName("Substring"), false, PrimitiveTypes.String, new IType[] { PrimitiveTypes.Int32, PrimitiveTypes.Int32 });
            MapPrimitiveMethod(substringMethod, (cg, caller) => new PartialLengthSliceBlock(caller));
            var substringStartMethod = PrimitiveTypes.String.GetMethod(new SimpleName("Substring"), false, PrimitiveTypes.String, new IType[] { PrimitiveTypes.Int32 });
            MapPrimitiveMethod(substringStartMethod, (cg, caller) => new PartialLengthSliceBlock(caller));

            #endregion
        }

        private static Dictionary<IMethod, Func<ICodeGenerator, IPythonBlock, IPythonBlock>> mappedPrimMethods;

        public static void MapPrimitiveMethod(IMethod SourceMethod, Func<ICodeGenerator, IPythonBlock, IPythonBlock> Target)
        {
            mappedPrimMethods[SourceMethod] = Target;
        }
        public static Func<ICodeGenerator, IPythonBlock, IPythonBlock> GetPrimitiveMethodAccessBuilder(IMethod SourceMethod)
        {
            return mappedPrimMethods[SourceMethod];
        }
        public static bool IsPrimitiveMethod(IMethod Method)
        {
            return mappedPrimMethods.ContainsKey(Method);
        }
        public static IPythonBlock CreatePrimitiveMethodAccess(ICodeGenerator CodeGenerator, IPythonBlock Caller, IMethod Method)
        {
            return GetPrimitiveMethodAccessBuilder(Method)(CodeGenerator, Caller);
        }
    }
}

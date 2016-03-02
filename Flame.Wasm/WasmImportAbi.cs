using System;
using System.Collections.Generic;
using Flame.Compiler.Native;
using Flame.Compiler;

namespace Flame.Wasm
{
    public class WasmImportAbi : IWasmCallAbi
    {
        public WasmImportAbi(IType PointerIntegerType, Func<IType, DataLayout> GetLayout)
        {
            this.PointerIntegerType = PointerIntegerType;
            this.getLayoutImpl = GetLayout;
        }

        private Func<IType, DataLayout> getLayoutImpl;

        /// <summary>
        /// Gets the integer type that is used to represent pointer values.
        /// </summary>
        public IType PointerIntegerType { get; private set; }

        /// <summary>
        /// Gets the given type's memory data layout.
        /// </summary>
        public DataLayout GetLayout(IType Type)
        {
            return getLayoutImpl(Type);
        }

        /// <summary>
        /// Gets the given method's signature, as a sequence of
        /// 'param' and 'result' expressions.
        /// </summary>
        public IEnumerable<WasmExpr> GetSignature(IMethod Method)
        {
            var results = new List<WasmExpr>();
            foreach (var parameter in Method.Parameters)
            {
                results.Add(WasmHelpers.DeclareParameter(parameter, this));
            }
            if (!Method.ReturnType.Equals(PrimitiveTypes.Void))
            {
                results.Add(WasmHelpers.DeclareResult(Method.ReturnType, this));
            }
            return results;
        }

        /// <summary>
        /// Creates a direct call to the given method. A 'this' pointer and
        /// a sequence of arguments are given.
        /// </summary>
        public IExpression CreateDirectCall(
            IMethod Target, IExpression ThisPointer, IEnumerable<IExpression> Arguments)
        {
            var args = new List<IExpression>();
            args.Add(ThisPointer);
            args.AddRange(Arguments);
            return new DirectCallExpression(
                OpCodes.CallImport, Target, Arguments);
        }
    }
}


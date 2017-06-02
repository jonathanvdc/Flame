using System;
using System.Collections.Generic;
using Flame.Compiler.Native;
using Flame.Compiler;
using Wasm;
using System.Linq;

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
        /// Gets the given method's signature.
        /// </summary>
        public FunctionType GetSignature(IMethod Method)
        {
            var signature = new FunctionType(
                Enumerable.Empty<WasmValueType>(),
                Enumerable.Empty<WasmValueType>());
            foreach (var parameter in Method.Parameters)
            {
                signature.ParameterTypes.Add(WasmHelpers.GetWasmValueType(parameter.ParameterType, this));
            }
            if (!Method.ReturnType.Equals(PrimitiveTypes.Void))
            {
                signature.ReturnTypes.Add(WasmHelpers.GetWasmValueType(Method.ReturnType, this));
            }
            return signature;
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
            return new DirectCallExpression(Target, Arguments);
        }
    }
}


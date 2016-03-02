using System;
using Flame.Compiler.Build;

namespace Flame.Wasm
{
    public class WasmAccessor : WasmMethod, IAccessor
    {
        public WasmAccessor(
            IProperty DeclaringProperty, IMethodSignatureTemplate Template, 
            AccessorType AccessorType, WasmModuleData ModuleData)
            : base(DeclaringProperty.DeclaringType, Template, ModuleData)
        {
            this.AccessorType = AccessorType;
            this.DeclaringProperty = DeclaringProperty;
        }

        public AccessorType AccessorType { get; private set; }
        public IProperty DeclaringProperty{ get; private set; }
    }
}


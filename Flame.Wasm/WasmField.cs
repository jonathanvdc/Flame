using System;
using Flame.Compiler;
using Flame.Compiler.Build;
using System.Collections.Generic;
using System.Linq;

namespace Flame.Wasm
{
    public class WasmField : IField, ILiteralField, IFieldBuilder
    {
        public WasmField(IType DeclaringType, IFieldSignatureTemplate Template, WasmModuleData ModuleData)
        {
            this.DeclaringType = DeclaringType;
            this.TemplateInstance = new FieldSignatureInstance(Template, this);
            this.ModuleData = ModuleData;
            this.Value = null;
        }

        public IType DeclaringType { get; private set; }
        public FieldSignatureInstance TemplateInstance { get; private set; }
        public IBoundObject Value { get; private set; }

        public WasmModuleData ModuleData { get; private set; }

        /// <summary>
        /// Gets this field's static storage location, assuming that it's
        /// a static field.
        /// </summary>
        public MemorySection StaticStorageLocation { get; private set; }

        public UnqualifiedName Name { get { return TemplateInstance.Name; } }
        public QualifiedName FullName { get { return Name.Qualify(DeclaringType.FullName); } }
        public bool IsStatic { get { return TemplateInstance.IsStatic; } }
        public AttributeMap Attributes { get { return TemplateInstance.Attributes.Value; } }
        public IType FieldType { get { return TemplateInstance.FieldType.Value; } }

        public bool TrySetValue(IExpression Value)
        {
            if (!IsStatic)
                return false;

            var val = Value.EvaluateOrNull();
            if (val != null)
            {
                this.Value = val;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Initialize()
        {
            if (IsStatic)
            {
                StaticStorageLocation = ModuleData.Memory.DeclareSection(
                    ModuleData.Abi.GetLayout(FieldType));
            }
        }

        public IField Build()
        {
            if (IsStatic && Value != null)
            {
                StaticStorageLocation.InitialData = GetBytes(Value);
            }
            return this;
        }

        private byte[] GetBytes(IBoundObject Object)
        {
            var ty = Object.Type;
            if (ty.GetIsPrimitive())
                return GetPrimitiveBytes(
                    ToClrPrimitive(Object.GetObjectValue()));

            var layout = ModuleData.Abi.GetLayout(ty);

            byte[] results = new byte[layout.Size];
            foreach (var item in layout.Members)
            {
                int offset = item.Value.Offset;
                byte[] fieldBytes = GetBytes(Object.GetField(item.Key));
                for (int i = 0; i < fieldBytes.Length; i++)
                {
                    results[offset + i] = fieldBytes[i];
                }
            }
            return results;
        }

        private static object ToClrPrimitive(object Value)
        {
            if (Value is IntegerValue)
                return ToClrPrimitive((IntegerValue)Value);
            else
                return Value;
        }

        private static object ToClrPrimitive(IntegerValue Value)
        {
            var spec = Value.Spec;
            if (spec.Equals(IntegerSpec.Int8))
                return Value.ToInt8();
            else if (spec.Equals(IntegerSpec.Int16))
                return Value.ToInt16();
            else if (spec.Equals(IntegerSpec.Int32))
                return Value.ToInt32();
            else if (spec.Equals(IntegerSpec.Int64))
                return Value.ToInt64();
            else if (spec.Equals(IntegerSpec.UInt8))
                return Value.ToUInt8();
            else if (spec.Equals(IntegerSpec.UInt16))
                return Value.ToUInt16();
            else if (spec.Equals(IntegerSpec.UInt32))
                return Value.ToUInt32();
            else if (spec.Equals(IntegerSpec.UInt64))
                return Value.ToUInt64();
            else
                throw new NotSupportedException("Unsupported integer spec: " + spec.ToString());
        }

        private static byte[] GetPrimitiveBytes(object Value)
        {
            var method = typeof(BitConverter)
                .GetMethod("GetBytes", new Type[] { Value.GetType() });

            if (method == null)
            {
                throw new InvalidOperationException(
                    "Can only get bytes of a primitive value.");
            }

            return (byte[])method.Invoke(null, new object[] { Value });
        }
    }
}

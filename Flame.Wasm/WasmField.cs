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

		public string Name { get { return TemplateInstance.Name; } }
		public string FullName { get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); } }
		public bool IsStatic { get { return TemplateInstance.IsStatic; } }
		public IEnumerable<IAttribute> Attributes { get { return TemplateInstance.Attributes.Value; } }
		public IType FieldType { get { return TemplateInstance.FieldType.Value; } }

		public void SetValue(IExpression Value)
		{
			this.Value = Value.EvaluateOrNull();
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
                return GetPrimitiveBytes(Object.GetObjectValue());

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


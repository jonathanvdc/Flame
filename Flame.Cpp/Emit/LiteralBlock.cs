using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class LiteralBlock : ICppBlock
    {
        public LiteralBlock(ICodeGenerator CodeGenerator, bool Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value ? "true" : "false";
            this.Type = PrimitiveTypes.Boolean;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, sbyte Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString(CultureInfo.InvariantCulture);
            this.Type = PrimitiveTypes.Int8;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, byte Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString(CultureInfo.InvariantCulture);
            this.Type = PrimitiveTypes.UInt8;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, short Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString(CultureInfo.InvariantCulture);
            this.Type = PrimitiveTypes.Int16;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, ushort Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString(CultureInfo.InvariantCulture);
            this.Type = PrimitiveTypes.UInt16;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, int Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString(CultureInfo.InvariantCulture);
            this.Type = PrimitiveTypes.Int32;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, uint Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString(CultureInfo.InvariantCulture);
            this.Type = PrimitiveTypes.UInt32;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, long Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString(CultureInfo.InvariantCulture);
            this.Type = PrimitiveTypes.Int64;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, ulong Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString(CultureInfo.InvariantCulture);
            this.Type = PrimitiveTypes.UInt64;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, float Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString(CultureInfo.InvariantCulture);
            this.Type = PrimitiveTypes.Float32;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, double Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString(CultureInfo.InvariantCulture);
            this.Type = PrimitiveTypes.Float64;
        }
        public LiteralBlock(ICodeGenerator CodeGenerator, string Value, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
            this.Type = Type;
        }

        public string Value { get; private set; }
        public IType Type { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(Value);
            return cb;
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[0]; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return new CppLocal[0]; }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }

    public static class LiteralExtensions
    {
        public static bool IsZeroLiteral(this ICppBlock Block)
        {
            if (Block is DoubleLiteralBlock)
            {
                return ((DoubleLiteralBlock)Block).Value == 0.0;
            }
            else if (Block is FloatLiteralBlock)
            {
                return ((FloatLiteralBlock)Block).Value == 0.0f;
            }
            else if (Block is LiteralBlock)
            {
                string val = ((LiteralBlock)Block).Value;
                double result;
                if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    return result == 0.0;
                }
            }
            return false;
        }
    }
}

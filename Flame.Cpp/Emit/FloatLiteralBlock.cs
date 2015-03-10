using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class FloatLiteralBlock : ICppBlock
    {
        public FloatLiteralBlock(ICodeGenerator CodeGenerator, float Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public float Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type
        {
            get { return PrimitiveTypes.Float32; }
        }

        public bool UseNumericLimits
        {
            get
            {
                return float.IsInfinity(Value) || float.IsNaN(Value);
            }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return UseNumericLimits ? new IHeaderDependency[] { StandardDependency.Limits } : Enumerable.Empty<IHeaderDependency>(); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Enumerable.Empty<CppLocal>(); }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            if (float.IsPositiveInfinity(Value))
            {
                cb.Append("std::numeric_limits<float>::infinity()");
            }
            else if (float.IsNegativeInfinity(Value))
            {
                cb.Append("-std::numeric_limits<float>::infinity()");
            }
            else if (float.IsNaN(Value))
            {
                cb.Append("-std::numeric_limits<float>::quiet_NaN()");
            }
            else
            {
                cb.Append(Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            return cb;
        }
    }
}

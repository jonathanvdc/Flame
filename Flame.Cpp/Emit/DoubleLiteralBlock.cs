using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class DoubleLiteralBlock : ICppBlock
    {
        public DoubleLiteralBlock(ICodeGenerator CodeGenerator, double Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public double Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type
        {
            get { return PrimitiveTypes.Float64; }
        }

        public bool UseNumericLimits
        {
            get
            {
                return double.IsInfinity(Value) || double.IsNaN(Value);
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
            if (double.IsPositiveInfinity(Value))
            {
                cb.Append("std::numeric_limits<double>::infinity()");
            }
            else if (double.IsNegativeInfinity(Value))
            {
                cb.Append("-std::numeric_limits<double>::infinity()");
            }
            else if (double.IsNaN(Value))
            {
                cb.Append("-std::numeric_limits<double>::quiet_NaN()");
            }
            else
            {
                cb.Append(Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            return cb;
        }
    }
}

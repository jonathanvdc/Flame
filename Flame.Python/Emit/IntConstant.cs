using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class IntConstant : IPythonBlock
    {
        public IntConstant(ICodeGenerator CodeGenerator, string Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }
        public IntConstant(ICodeGenerator CodeGenerator, int Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString();
        }
        public IntConstant(ICodeGenerator CodeGenerator, ulong Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString();
        }
        public IntConstant(ICodeGenerator CodeGenerator, long Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value.ToString();
        }

        public string Value { get; private set; }

        public bool IsZero
        {
            get { return Value == "0"; }
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Value);
        }

        public IType Type
        {
            get { return PrimitiveTypes.Int32; }
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return new ModuleDependency[0];
        }
    }

    public class FloatConstant : IPythonBlock
    {
        public FloatConstant(ICodeGenerator CodeGenerator, double Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public double Value { get; private set; }

        public bool IsZero
        {
            get { return Value == 0; }
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {            
            if (double.IsNaN(Value))
            {
                return new CodeBuilder("float(\"nan\")");
            }
            else if (double.IsPositiveInfinity(Value))
            {
                return new CodeBuilder("float(\"inf\")");
            }
            else if (double.IsNegativeInfinity(Value))
            {
                return new CodeBuilder("float(\"-inf\")");
            }
            else
            {
                string repr = Value.ToString(); 
                if (!(repr.IndexOf("E") > -1 || repr.IndexOf("e") > -1 || repr.IndexOf(".") > -1))
                {
                    return new CodeBuilder(repr + ".0");
                }
                else
                {
                    return new CodeBuilder(repr);
                }
            }
        }

        public IType Type
        {
            get { return PrimitiveTypes.Float64; }
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return new ModuleDependency[0];
        }
    }
}

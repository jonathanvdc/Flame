using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class KeywordBlock : IPythonBlock
    {
        public KeywordBlock(ICodeGenerator CodeGenerator, string Keyword, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Keyword = Keyword; 
            this.Type = Type;
        }

        public string Keyword { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type { get; private set; }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Keyword);
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return new ModuleDependency[0];
        }
    }
    public class PythonIdentifierBlock : IPythonBlock
    {
        public PythonIdentifierBlock(ICodeGenerator CodeGenerator, string Value, IType Type, params ModuleDependency[] Dependencies)
            : this(CodeGenerator, Value, Type, (IEnumerable<ModuleDependency>)Dependencies)
        {
        }
        public PythonIdentifierBlock(ICodeGenerator CodeGenerator, string Value, IType Type, IEnumerable<ModuleDependency> Dependencies)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
            this.Type = Type;
            this.Dependencies = Dependencies;
        }

        public string Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type { get; private set; }
        public IEnumerable<ModuleDependency> Dependencies { get; private set; }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Value);
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Dependencies;
        }
    }
}

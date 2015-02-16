using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class KeywordStatementBlock : ICppBlock
    {
        public KeywordStatementBlock(ICodeGenerator CodeGenerator, string Keyword)
        {
            this.CodeGenerator = CodeGenerator;
            this.Keyword = Keyword;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public string Keyword { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[0]; }
        }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Keyword + ";");
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
}

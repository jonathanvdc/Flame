using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ExpressionStatementBlock : ICppBlock
    {
        public ExpressionStatementBlock(ICppBlock Expression)
        {
            this.Expression = Expression;
        }

        public ICppBlock Expression { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Expression.Dependencies; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Expression.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = Expression.GetCode();
            if (!cb.IsWhitespace)
            {
                cb.Append(';');
            }
            return cb;
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Expression.LocalsUsed; }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}

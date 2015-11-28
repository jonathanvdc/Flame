using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class LambdaHeaderBlock : ILambdaHeaderBlock
    {
        public LambdaHeaderBlock(AssemblyRecompiler Recompiler, LambdaHeader Header)
        {
            this.Header = Header;
            this.LambdaCodeGenerator = new RecompiledCodeGenerator(Recompiler, this.Header.Signature);
            this.BoundHeaderBlock = new LambdaBoundHeaderBlock();
        }

        public LambdaHeader Header { get; private set; }
        public LambdaBoundHeaderBlock BoundHeaderBlock { get; private set; }
        public ICodeGenerator LambdaCodeGenerator { get; private set; }

        public ICodeBlock ThisLambdaBlock
        {
            get
            {
                return new ExpressionBlock(LambdaCodeGenerator, new LambdaDelegateExpression(Header, BoundHeaderBlock));
            }
        }

        public ICodeBlock EmitGetCapturedValue(int Index)
        {
            return new ExpressionBlock(LambdaCodeGenerator, new LambdaCapturedValueExpression(Header, BoundHeaderBlock, Index));
        }

        public LambdaExpression CreateLambda(IStatement Body)
        {
            return new LambdaExpression(Header, Body, BoundHeaderBlock);
        }
    }
}

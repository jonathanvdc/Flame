using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class AssemblerArgument : IVariable
    {
        public AssemblerArgument(ICodeGenerator CodeGenerator, int Index)
        {
            this.CodeGenerator = CodeGenerator;
            this.Index = Index;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public int Index { get; private set; }

        public IExpression CreateGetExpression()
        {
            var t = Type;
            return new CodeBlockExpression(new FunctionAssemblerBlock(CodeGenerator, t, (context) =>
            {
                var arg = context.GetArgument(Index);
                return new IStorageLocation[] { arg };
            }), t);
        }

        public IStatement CreateReleaseStatement()
        {
            return new CodeBlockStatement(new EmptyBlock(CodeGenerator));
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            var valBlock = (IAssemblerBlock)Value.Emit(CodeGenerator);
            return new CodeBlockStatement(new ActionAssemblerBlock(CodeGenerator, (context) =>
            {
                var arg = context.GetArgument(Index);
                valBlock.EmitStoreTo(arg, context);
            }));
        }

        public IType Type
        {
            get { return CodeGenerator.Method.GetParameters()[Index].ParameterType; }
        }
    }
}

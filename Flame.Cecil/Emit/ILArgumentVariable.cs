using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ILArgumentVariable : IUnmanagedVariable
    {
        public ILArgumentVariable(ICodeGenerator CodeGenerator, int Index)
        {
            this.CodeGenerator = CodeGenerator;
            this.Index = Index;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public int Index { get; private set; }
        public IType Type { get { return ILCodeGenerator.GetExtendedParameterTypes(CodeGenerator)[Index]; } }

        public IExpression CreateAddressOfExpression()
        {
            return new CodeBlockExpression(new ArgumentAddressOfBlock(CodeGenerator, Index), Type.MakePointerType(PointerKind.ReferencePointer));
        }

        public IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(new ArgumentGetBlock(CodeGenerator, Index), Type);
        }

        public IStatement CreateReleaseStatement()
        {
            return new EmptyStatement();
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new CodeBlockStatement(new ArgumentSetBlock(CodeGenerator, Index, (ICecilBlock)Value.Emit(CodeGenerator)));
        }
    }
}

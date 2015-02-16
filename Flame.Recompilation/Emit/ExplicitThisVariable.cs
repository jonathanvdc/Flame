using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    /*public class ExplicitThisVariable : IUnmanagedVariable
    {
        public ExplicitThisVariable(IMethod DeclaringMethod)
        {
            var genDeclType = DeclaringMethod.DeclaringType;
            var declType = genDeclType.get_IsGeneric() ? genDeclType.MakeGenericType(genDeclType.GetGenericParameters()) : genDeclType;
            if (declType.get_IsValueType() && !declType.get_IsPointer())
            {
                this.Type = declType.MakePointerType(PointerKind.ReferencePointer);
            }
            else
            {
                this.Type = declType;
            }
        }
        public ExplicitThisVariable(IType Type)
        {
            this.Type = Type;
        }

        public IType Type { get; private set; }

        public IExpression CreateAddressOfExpression()
        {
            return new ThisAddressOfExpression(Type.MakePointerType(PointerKind.ReferencePointer));
        }

        public IExpression CreateGetExpression()
        {
            return new ThisGetExpression(Type);
        }

        public IStatement CreateReleaseStatement()
        {
            return new EmptyStatement();
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new ThisSetStatement(Value);
        }
    }*/
}

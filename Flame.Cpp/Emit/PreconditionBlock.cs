using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class PreconditionBlock : ICppBlock
    {
        public PreconditionBlock(ICppBlock Precondition)
        {
            this.Precondition = Precondition;
        }

        public ICppBlock Precondition { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Precondition.Dependencies.MergeDependencies(new IHeaderDependency[] { Plugs.ContractsHeader.Instance }); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Precondition.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Precondition.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            var method = CodeGenerator.EmitMethod(CppPrimitives.RequireMethod, null, Operator.GetDelegate);
            var cppExpr = (ICppBlock)CodeGenerator.EmitInvocation(method, new ICodeBlock[] { Precondition });
            return new ExpressionStatementBlock(cppExpr).GetCode();
        }
    }
}

using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class PostconditionBlock : ICppBlock
    {
        public PostconditionBlock(ICppBlock Postcondition)
        {
            this.Postcondition = Postcondition;
        }

        public ICppBlock Postcondition { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Postcondition.Dependencies.MergeDependencies(new IHeaderDependency[] { Plugs.ContractsHeader.Instance }); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Postcondition.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Postcondition.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            var method = CodeGenerator.EmitMethod(CppPrimitives.EnsureMethod, null, Operator.GetDelegate);
            var cppExpr = (ICppBlock)CodeGenerator.EmitInvocation(method, new ICodeBlock[] { Postcondition });
            return new ExpressionStatementBlock(cppExpr).GetCode();
        }
    }
}

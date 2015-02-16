using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ScopeOperatorBlock : ICppBlock
    {
        public ScopeOperatorBlock(ICppBlock Left, ICppBlock Right)
        {
            this.Left = Left;
            this.Right = Right;
        }

        public ICppBlock Left { get; private set; }
        public ICppBlock Right { get; private set; }

        public IType Type { get { return Right.Type; } }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Left.Dependencies.MergeDependencies(Right.Dependencies); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Left.LocalsUsed.Concat(Right.LocalsUsed).Distinct(); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Left.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = Left.GetCode();
            cb.Append("::");
            cb.Append(Right.GetCode());
            return cb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}

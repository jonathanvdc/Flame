using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    /// <summary>
    /// A block that emits code for certain type arguments to be attached to a base block.
    /// Its type is assumed to be that of the base block.
    /// </summary>
    public class TypeArgumentBlock : ICppBlock
    {
        public TypeArgumentBlock(ICppBlock Block, IEnumerable<ICppBlock> TypeArguments)
        {
            this.Block = Block;
            this.TypeArguments = TypeArguments;
        }

        public ICppBlock Block { get; private set; }
        public IEnumerable<ICppBlock> TypeArguments { get; private set; }

        public IType Type
        {
            get 
            {
                return Block.Type;
            }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Block.Dependencies.MergeDependencies(TypeArguments.SelectMany((item) => item.Dependencies)); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Block.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Block.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = Block.GetCode();
            if (TypeArguments.Any())
            {
                cb.Append("<");
                cb.Append(TypeArguments.First().GetCode());
                foreach (var item in TypeArguments.Skip(1))
                {
                    cb.Append(", ");
                    cb.Append(item.GetCode());
                }
                cb.Append(">");
            }
            return cb;
        }
    }
}

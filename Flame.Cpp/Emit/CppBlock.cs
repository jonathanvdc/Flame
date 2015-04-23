using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppBlock : ICppLocalDeclaringBlock, IMultiBlock
    {
        public CppBlock(ICodeGenerator CodeGenerator, IReadOnlyList<ICppBlock> Blocks)
        {
            this.CodeGenerator = CodeGenerator;
            this.Blocks = Blocks;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IReadOnlyList<ICppBlock> Blocks { get; private set; }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return Blocks.GetLocalDeclarations(); }
        }

        public IType Type
        {
            get
            {
                return Blocks.Select(item => item.Type).Where(item => !item.Equals(PrimitiveTypes.Void)).LastOrDefault() ?? PrimitiveTypes.Void;
            }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Blocks.Aggregate(Enumerable.Empty<IHeaderDependency>(), (acc, item) => acc.Union(item.Dependencies)); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Blocks.Aggregate(Enumerable.Empty<CppLocal>(), (acc, item) => acc.Union(item.LocalsUsed)); }
        }

        public int StatementCount
        {
            get
            {
                int count = 0;
                foreach (var item in Blocks)
                {
                    if (!(item is LocalDeclarationReference) || !((LocalDeclarationReference)item).IsEmpty)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public CodeBuilder GetCode()
        {
            var codes = this.Flatten()
                              .Select(item => item.GetCode())
                              .Where(item => !item.IsWhitespace)
                              .ToArray();

            if (codes.Length == 0)
            {
                return new CodeBuilder(";");
            }
            else if (codes.Length == 1)
            {
                return codes[0];
            }
            else
            {
                CodeBuilder cb = new CodeBuilder();
                cb.AddLine("{");
                cb.IncreaseIndentation();
                foreach (var item in codes)
                {
                    cb.AddCodeBuilder(item);
                }
                cb.DecreaseIndentation();
                cb.AddLine("}");
                return cb;
            }
        }

        public IEnumerable<ICppBlock> GetBlocks()
        {
            return Blocks;
        }

        /*public override string ToString()
        {
            return GetCode().ToString();
        }*/
    }
}

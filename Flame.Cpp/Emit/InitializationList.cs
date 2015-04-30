using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    /// <summary>
    /// Represents a constructor initialization list.
    /// </summary>
    public class InitializationList : ICppBlock
    {
        public InitializationList(ICodeGenerator CodeGenerator, IEnumerable<MemberInitializationBlock> Initializations)
        {
            this.CodeGenerator = CodeGenerator;
            this.Initializations = Initializations;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IEnumerable<MemberInitializationBlock> Initializations { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Initializations.Aggregate(Enumerable.Empty<IHeaderDependency>(), (acc, item) => acc.MergeDependencies(item.Dependencies)); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Initializations.Aggregate(Enumerable.Empty<CppLocal>(), (acc, item) => acc.Union(item.LocalsUsed)); }
        }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            if (Initializations.Any())
            {
                cb.IncreaseIndentation();
                cb.Append(": ");
                cb.Append(Initializations.First().GetCode());
                foreach (var item in Initializations.Skip(1))
                {
                    cb.Append(", ");
                    cb.Append(item.GetCode());
                }
                cb.DecreaseIndentation();
            }
            return cb;
        }
    }
}

using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class InitializerListBlock : ICppBlock
    {
        public InitializerListBlock(ICodeGenerator CodeGenerator, IType ElementType, IEnumerable<ICppBlock> Elements)
        {
            this.CodeGenerator = CodeGenerator;
            this.ElementType = ElementType;
            this.Elements = Elements;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IType ElementType { get; private set; }
        public IEnumerable<ICppBlock> Elements { get; private set; }
        
        public IType Type
        {
            get { return Plugs.StdInitializerList.Instance.MakeGenericType(new IType[] { ElementType }); }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Elements.SelectMany(item => item.Dependencies).With(StandardDependency.InitializerList).Distinct(); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Elements.SelectMany(item => item.LocalsUsed).Distinct(); }
        }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            cb.Append("{");
            if (Elements.Any())
            {
                cb.Append(" ");
                cb.Append(Elements.First().GetCode());
                foreach (var item in Elements.Skip(1))
                {
                    cb.Append(", ");
                    cb.Append(item.GetCode());
                }
            }
            cb.Append(" }");
            return cb;
        }
    }
}

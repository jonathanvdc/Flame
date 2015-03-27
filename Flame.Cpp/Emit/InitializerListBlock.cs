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

            int maxLength = CodeGenerator.GetEnvironment().Log.Options.GetOption<int>("list-line-length", 30);
            var elemCodeBuilders = Elements.Select(item => item.GetCode()).ToArray();
            int totalLength = elemCodeBuilders.Aggregate(0, (length, item) => length + item.LastCodeLine.Text.Length);
            bool breakLines = totalLength > maxLength;

            cb.Append("{");
            if (breakLines)
            {
                cb.IncreaseIndentation();
                cb.AppendLine();
            }
            if (Elements.Any())
            {
                if (!breakLines)
                {
                    cb.Append(" ");
                }
                cb.Append(elemCodeBuilders.First());
                int currentLength = elemCodeBuilders[0].LastCodeLine.Text.Length;
                foreach (var item in elemCodeBuilders.Skip(1))
                {
                    cb.Append(", ");
                    if (currentLength > maxLength)
                    {
                        cb.AppendLine();
                        currentLength = 0;
                    }
                    cb.Append(item);
                    currentLength += item.LastCodeLine.Text.Length;
                }
            }
            if (breakLines)
            {
                cb.DecreaseIndentation();
                cb.AppendLine();
            }
            else
            {
                cb.Append(" ");
            }
            cb.Append("}");
            return cb;
        }
    }
}

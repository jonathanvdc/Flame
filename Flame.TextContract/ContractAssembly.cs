using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractAssembly : IAssemblyBuilder
    {
        public ContractAssembly(string Name, IEnvironment Environment)
        {
            this.Name = Name;
            this.Environment = Environment;
            this.mainNs = new ContractNamespace(this, "", "");
        }

        public IEnvironment Environment { get; private set; }
        private ContractNamespace mainNs;

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return mainNs.DeclareNamespace(Name);
        }

        public void Save(System.IO.Stream Stream)
        {
            var cb = mainNs.GetCode();
            cb.IndentationString = new string(' ', 4);
            string code = cb.ToString();
            using (StreamWriter writer = new StreamWriter(Stream))
            {
                writer.Write(code);
            }
        }

        public void SetEntryPoint(IMethod Method)
        {
        }

        public IAssembly Build()
        {
            return this;
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }

        public string FullName
        {
            get { return Name; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[0]; }
        }

        public string Name { get; private set; }

        public Version AssemblyVersion
        {
            get { return new Version(1, 0, 0, 0); }
        }

        public IBinder CreateBinder()
        {
            return new Flame.Binding.NamespaceTreeBinder(Environment, mainNs);
        }

        public IMethod GetEntryPoint()
        {
            return null;
        }

        public void Save(IOutputProvider OutputProvider)
        {
            if (OutputProvider.PreferSingleOutput)
            {
                using (var stream = OutputProvider.Create().OpenOutput())
                {
                    Save(stream);
                }
            }
            else
            {
                foreach (var item in this.CreateBinder().GetTypes())
                {
                    var node = (ISyntaxNode)item;
                    var cb = node.GetCode();
                    cb.IndentationString = "    ";
                    string code = cb.ToString();
                    using (var stream = OutputProvider.Create(item.GetGenericFreeName(), "txt").OpenOutput())
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(code);
                    }
                }
            }
        }
    }
}

using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public class AssemblerFile
    {
        public AssemblerFile(AssemblerAssembly Assembly)
        {
            this.Assembly = Assembly;
            this.Methods = Assembly.AllTypes.SelectMany((item) => item.GetMethods().OfType<AssemblerMethod>()).ToArray();
        }
        public AssemblerFile(AssemblerAssembly Assembly, IEnumerable<AssemblerMethod> Methods)
        {
            this.Assembly = Assembly;
            this.Methods = Methods;
        }

        public AssemblerAssembly Assembly { get; private set; }
        public IEnumerable<AssemblerMethod> Methods { get; private set; }
        
        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.IndentationString = new string(' ', 4);
            foreach (var item in Methods)
            {
                if (item.IsGlobal)
                {
                    cb.AddLine(".globl " + item.Label.Identifier);
                }
            }
            cb.AddEmptyLine();
            foreach (var item in Methods)
            {
                cb.AddCodeBuilder(item.GetCode());
                cb.AddEmptyLine();
            }
            return cb;
        }

        public void Save(IOutputProvider Output)
        {
            string code = GetCode().ToString();
            using (var stream = Output.Create(Assembly.Name, "asm").OpenOutput())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(code);
            }
        }
    }
}

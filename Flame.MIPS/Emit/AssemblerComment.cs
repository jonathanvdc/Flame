using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class AssemblerComment : IAssemblerCode
    {
        public AssemblerComment(string Comment)
        {
            this.Comment = Comment;
        }

        public string Comment { get; private set; }

        public CodeBuilder GetCode()
        {
            if (string.IsNullOrWhiteSpace(Comment))
            {
                CodeBuilder cb = new CodeBuilder();
                cb.AddEmptyLine();
                return cb;
            }
            else
            {
                return new CodeBuilder("# " + Comment);
            }
        }
    }
}

using Flame.Compiler;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonDecorator : ISyntaxNode
    {
        public PythonDecorator(string Name)
        {
            this.Name = Name;
        }

        public string Name { get; private set; }

        public override string ToString()
        {
            return "@" + Name;
        }
        public CodeBuilder GetCode()
        {
            return new CodeBuilder(ToString());
        }
        public override bool Equals(object obj)
        {
            if (obj is PythonDecorator)
            {
                return Name.Equals(((PythonDecorator)obj).Name);
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public static bool operator ==(PythonDecorator A, PythonDecorator B)
        {
            return A.Name == B.Name;
        }
        public static bool operator !=(PythonDecorator A, PythonDecorator B)
        {
            return !(A == B);
        }
    }
}

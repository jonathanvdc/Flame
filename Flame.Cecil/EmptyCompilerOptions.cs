using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class EmptyCompilerOptions : ICompilerOptions
    {
        public EmptyCompilerOptions()
        {

        }

        public T GetOption<T>(string Key, T Default)
        {
            return Default;
        }

        public bool HasOption(string Key)
        {
            return false;
        }
    }
}

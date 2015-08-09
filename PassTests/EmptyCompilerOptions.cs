using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassTests
{
    public class EmptyCompilerOptions : ICompilerOptions
    {
        private EmptyCompilerOptions() { }

        static EmptyCompilerOptions()
        {
            Instance = new EmptyCompilerOptions();
        }

        public static EmptyCompilerOptions Instance { get; private set; }

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

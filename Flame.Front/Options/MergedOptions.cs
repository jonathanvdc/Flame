using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    public class MergedOptions : ICompilerOptions
    {
        public MergedOptions(ICompilerOptions MainOptions, ICompilerOptions ExtraOptions)
        {
            this.MainOptions = MainOptions;
            this.ExtraOptions = ExtraOptions;
        }

        public ICompilerOptions MainOptions { get; private set; }
        public ICompilerOptions ExtraOptions { get; private set; }

        public T GetOption<T>(string Key, T Default)
        {
            if (MainOptions.HasOption(Key))
            {
                return MainOptions.GetOption<T>(Key, Default);
            }
            else
            {
                return ExtraOptions.GetOption<T>(Key, Default);
            }
        }

        public bool HasOption(string Key)
        {
            return MainOptions.HasOption(Key) || ExtraOptions.HasOption(Key);
        }
    }
}

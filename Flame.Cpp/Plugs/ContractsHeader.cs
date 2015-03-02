using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class ContractsHeader : IHeaderDependency
    {
        private ContractsHeader()
        {

        }

        private static ContractsHeader inst;
        public static ContractsHeader Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new ContractsHeader();
                }
                return inst;
            }
        }

        private const string Code =
@"#include <cassert>

#define require assert
#define ensure assert
";

        public void Include(IOutputProvider OutputProvider)
        {
            var handle = OutputProvider.Create("Contracts", "h");
            using (var stream = handle.OpenOutput())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(Code);
            }
        }

        public bool IsStandard
        {
            get { return false; }
        }

        public string HeaderName
        {
            get { return "Contracts.h"; }
        }
    }
}

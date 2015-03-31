using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class DefaultConsole : ConsoleBase
    {
        public override ConsoleDescription Description
        {
            get { return new ConsoleDescription("default"); }
        }
    }
}

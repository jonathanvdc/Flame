using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class SingleThreadedTaskManager : IAsyncTaskManager
    {
        public async Task RunAsync(Action Method)
        {
            Method();
        }

        public async Task WhenDoneAsync()
        {
        }
    }
}

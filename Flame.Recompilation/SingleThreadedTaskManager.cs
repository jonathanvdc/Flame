using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class SingleThreadedTaskManager : IAsyncTaskManager
    {
        public Task RunAsync(Action Method)
        {
            Method();
            return Task.FromResult(true);
        }

        public Task WhenDoneAsync()
        {
            return Task.FromResult(true);
        }

        public void RunSequential(Action Delegate)
        {
            Delegate();
        }

        public Task<T> RunAsync<T>(Func<T> Delegate)
        {
            return Task.FromResult<T>(Delegate());
        }
    }
}

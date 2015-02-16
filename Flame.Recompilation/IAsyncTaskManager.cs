using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public interface IAsyncTaskManager
    {
        Task RunQueued();
        void QueueAction(Action Method);
        Task WhenDoneAsync();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class SingleThreadedTaskManager : IAsyncTaskManager
    {
        public SingleThreadedTaskManager()
        {
            this.actions = new Queue<Action>();
        }

        private Queue<Action> actions;

        public void QueueAction(Action Method)
        {
            actions.Enqueue(Method);
        }

        public async Task WhenDoneAsync()
        {
        }

        public async Task RunQueued()
        {
            while (actions.Count > 0)
            {
                actions.Dequeue()();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class AsyncTaskManager : IAsyncTaskManager
    {
        public AsyncTaskManager()
        {
            this.tasks = new List<Task>();
            this.MillisecondsDelay = 100;
        }
        public AsyncTaskManager(int MillisecondsDelay)
        {
            this.tasks = new List<Task>();
            this.MillisecondsDelay = MillisecondsDelay;
        }

        public int MillisecondsDelay { get; private set; }

        private List<Task> tasks;

        public event EventHandler TasksDone;

        protected void OnTasksDone()
        {
            if (TasksDone != null)
            {
                TasksDone(this, EventArgs.Empty);
            }
        }

        private bool IsDone
        {
            get
            {
                lock (tasks)
                {
                    return tasks.Count == 0;
                }
            }
        }

        private void CleanTasks()
        {
            lock (tasks)
            {
                for (int i = 0; i < tasks.Count; )
                {
                    if (tasks[i].IsCompleted)
                    {
                        tasks.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            if (IsDone)
            {
                OnTasksDone();
            }
        }

        public Task WhenDoneAsync()
        {
            var task = new TaskCompletionSource<bool>();
            TasksDone += (o, e) => task.SetResult(true);
            return task.Task;
        }

        public Task RunAsync(Action Method)
        {
            lock (tasks)
            {
                var task = Task.Run(Method);
                tasks.Add(task);
                return task;
            }
        }
    }
}

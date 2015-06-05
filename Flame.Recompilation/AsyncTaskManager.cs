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
            this.sequentialLock = new object();
        }

        private List<Task> tasks;
        private object sequentialLock;

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

        public Task RunAsync(Action Delegate)
        {
            lock (tasks)
            {
                var task = Task.Run(Delegate);
                tasks.Add(task);
                return task;
            }
        }

        public void RunSequential(Action Delegate)
        {
            lock (sequentialLock)
            {
                Delegate();
            }
        }

        public Task<T> RunAsync<T>(Func<T> Delegate)
        {
            lock (tasks)
            {
                var task = Task.Run(Delegate);
                tasks.Add(task);
                return task;
            }
        }
    }
}

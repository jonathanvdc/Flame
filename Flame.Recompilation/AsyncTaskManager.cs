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
            this.actions = new Queue<Action>();
            this.MillisecondsDelay = 100;
        }
        public AsyncTaskManager(int MillisecondsDelay)
        {
            this.tasks = new List<Task>();
            this.actions = new Queue<Action>();
            this.MillisecondsDelay = MillisecondsDelay;
        }

        public int MillisecondsDelay { get; private set; }

        private Queue<Action> actions;
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
                lock (actions)
                {
                    return actions.Count == 0 && tasks.Count == 0;
                }
            }
        }

        private void CleanTasks()
        {
            lock (actions)
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

        public async Task RunQueued()
        {
            Task[] currentTasks;
            lock (actions)
            {
                currentTasks = new Task[actions.Count];
                for (int i = 0; i < currentTasks.Length; i++)
                {
                    var task = Task.Run(actions.Dequeue());
                    tasks.Add(task);
                    currentTasks[i] = task;
                }
            }
            await Task.WhenAll(currentTasks);
            CleanTasks();
        }

        public Task WhenDoneAsync()
        {
            var task = new TaskCompletionSource<bool>();
            TasksDone += (o, e) => task.SetResult(true);
            return task.Task;
        }

        public void QueueAction(Action Method)
        {
            lock (actions)
            {
                actions.Enqueue(Method);
            }
        }
    }
}

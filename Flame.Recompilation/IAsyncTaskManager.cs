using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public interface IAsyncTaskManager
    {
        Task RunAsync(Action Delegate);
        Task<T> RunAsync<T>(Func<T> Delegate);
        Task WhenDoneAsync();
        void RunSequential(Action Delegate);
    }

    public static class AsyncTaskManagerExtensions
    {
        public static void RunSequential<T>(this IAsyncTaskManager Manager, Action<T> Delegate, T Argument)
        {
            Manager.RunSequential(() => Delegate(Argument));
        }
        public static T RunSequential<T>(this IAsyncTaskManager Manager, Func<T> Delegate)
        {
            var result = default(T);
            Manager.RunSequential(() => { result = Delegate(); });
            return result;
        }
        public static T2 RunSequential<T1, T2>(this IAsyncTaskManager Manager, Func<T1, T2> Delegate, T1 Argument)
        {
            var result = default(T2);
            Manager.RunSequential(() => { result = Delegate(Argument); });
            return result;
        }
    }
}

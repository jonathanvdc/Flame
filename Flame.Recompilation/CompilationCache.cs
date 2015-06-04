using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public struct MemberCreationResult<T>
    {
        public MemberCreationResult(T Member)
        {
            this = default(MemberCreationResult<T>);
            this.Member = Member;
        }
        public MemberCreationResult(T Member, Action<T, T> Continuation)
        {
            this = default(MemberCreationResult<T>);
            this.Member = Member;
            this.Continuation = Continuation;
        }

        public T Member { get; private set; }
        public Action<T, T> Continuation { get; private set; }

        public static implicit operator MemberCreationResult<T>(T Value)
        {
            return new MemberCreationResult<T>(Value);
        }
    }

    /// <summary>
    /// Represents a thread-safe compilation cache.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CompilationCache<T>
    {
        public CompilationCache(Func<T, MemberCreationResult<T>> GetNew, IAsyncTaskManager TaskManager)
        {
            this.getNew = GetNew;
            this.cache = new Dictionary<T, T>();
            this.TaskManager = TaskManager;
        }

        private Func<T, MemberCreationResult<T>> getNew;
        private Dictionary<T, T> cache;
        public IAsyncTaskManager TaskManager { get; private set; }

        private T GetCore(T Source)
        {
            if (cache.ContainsKey(Source))
            {
                return cache[Source];
            }
            else
            {
                var result = getNew(Source);
                cache[Source] = result.Member;
                if (result.Continuation != null)
                {
                    result.Continuation(result.Member, Source);
                }
                return result.Member;
            }
        }

        public T GetOriginal(T Value)
        {
            lock (cache)
            {
                foreach (var item in cache)
                {
                    if (Value.Equals(item.Value))
                    {
                        return item.Key;
                    }
                }
                return default(T);
            }
        }

        public T[] GetAll()
        {
            lock (cache)
            {
                return cache.Values.ToArray();
            }
        }

        public T Get(T Source)
        {
            T result;
            lock (cache)
            {
                result = GetCore(Source);
            }
            TaskManager.RunQueued();
            return result;
        }
        public T[] GetMany(params T[] Source)
        {
            T[] results = new T[Source.Length];
            lock (cache)
            {
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = GetCore(Source[i]);
                }
            }
            TaskManager.RunQueued();
            return results;
        }
        public T[] GetMany(IEnumerable<T> Source)
        {
            return GetMany(Source.ToArray());
        }

        public T FirstOrDefault(Func<T, bool> Query)
        {
            bool success;
            return First(Query, out success);
        }
        public T First(Func<T, bool> Query, out bool Success)
        {
            lock (cache)
            {
                foreach (var item in cache.Values)
                {
                    if (Query(item))
                    {
                        Success = true;
                        return item;
                    }
                }
            }
            Success = false;
            return default(T);
        }
    }
}

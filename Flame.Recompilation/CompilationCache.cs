using System;
using System.Collections.Concurrent;
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
            this.cache = new ConcurrentDictionary<T, T>();
            this.TaskManager = TaskManager;
        }

        private Func<T, MemberCreationResult<T>> getNew;
        private ConcurrentDictionary<T, T> cache;
        public IAsyncTaskManager TaskManager { get; private set; }

        private T GetCore(T Source)
        {
            var result = TaskManager.RunSequential(getNew, Source);
            cache[Source] = result.Member;
            if (result.Continuation != null)
            {
                result.Continuation(result.Member, Source);
            }
            return result.Member;
        }

        public T GetOriginal(T Value)
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

        public IEnumerable<T> GetAll()
        {
            return cache.Values;
        }

        public T Get(T Source)
        {
            return cache.GetOrAdd(Source, GetCore);
        }
        public T[] GetMany(params T[] Source)
        {
            return Source.Select(Get).ToArray();
        }
        public IEnumerable<T> GetMany(IEnumerable<T> Source)
        {
            return Source.Select(Get);
        }

        public bool ContainsKey(T Source)
        {
            return cache.ContainsKey(Source);
        }

        public bool TryGet(T Source, out T Result)
        {
            return cache.TryGetValue(Source, out Result);
        }

        public T FirstOrDefault(Func<T, bool> Query)
        {
            T result;
            First(Query, out result);
            return result;
        }
        public bool First(Func<T, bool> Query, out T Result)
        {
            foreach (var item in cache.Values)
            {
                if (Query(item))
                {
                    Result = item;
                    return true;
                }
            }
            Result = default(T);
            return false;
        }
    }
}

using System;

namespace Flame.Build.Lazy
{
    /// <summary>
    /// A type of object that guarantees thread-safe, on-demand
    /// initialization. An initialization action is only executed once.
    /// </summary>
    public struct DeferredInitializer<T>
    {
        /// <summary>
        /// Creates a new deferred initializer object that executes the given
        /// initialization action.
        /// </summary>
        public DeferredInitializer(Action<T> Initializer)
        {
            this.init = Initializer;
            this.syncObject = new object();
        }

        private Action<T> init;
        private object syncObject;

        /// <summary>
        /// Gets a boolean that determines whether this instance has
        /// initialized an object or not.
        /// </summary>
        /// <value><c>true</c> if this instance has initialized; otherwise, <c>false</c>.</value>
        public bool HasInitialized { get { return syncObject == null; } }

        /// <summary>
        /// Initializes the given instance. Note that this initializer
        /// can only be used to initialize an object once.
        /// A boolean is returned that tells whether the instance
        /// was actually initialized or not.
        /// </summary>
        public bool Initialize(T Instance)
        {
            if (syncObject == null)
                return false;

            bool result = false;
            lock (syncObject)
            {
                if (init != null)
                {
                    var f = init;
                    init = null;
                    f(Instance);
                    result = true;
                }
            }
            syncObject = null;
            return result;
        }
    }
}

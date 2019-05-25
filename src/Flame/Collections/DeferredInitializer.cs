using System;

namespace Flame.Collections
{
    /// <summary>
    /// A type of object that guarantees thread-safe, on-demand
    /// initialization. An initialization action is only executed once.
    /// </summary>
    public struct DeferredInitializer
    {
        /// <summary>
        /// Creates a deferred initializer that executes a particular
        /// initialization action once.
        /// </summary>
        /// <param name="initializer">The initializer to run once.</param>
        private DeferredInitializer(Action initializer)
        {
            this.init = initializer;
            this.syncObject = new object();
        }

        /// <summary>
        /// Creates a deferred initializer that executes a particular
        /// initialization action once.
        /// </summary>
        /// <param name="initializer">The initializer to run once.</param>
        /// <returns>
        /// A deferred initializer.
        /// </returns>
        public static DeferredInitializer Create(Action initializer)
        {
            return new DeferredInitializer(initializer);
        }

        /// <summary>
        /// Creates a deferred initializer that runs a particular
        /// initializer once on a particular value.
        /// </summary>
        /// <param name="instance">A value to initialize.</param>
        /// <param name="initializer">The initializer to run.</param>
        /// <returns>
        /// A deferred initializer.
        /// </returns>
        public static DeferredInitializer Create<T>(
            T instance,
            Action<T> initializer)
        {
            return Create(() => initializer(instance));
        }

        private Action init;
        private object syncObject;

        /// <summary>
        /// Gets a boolean that determines whether the initializer has run or not.
        /// </summary>
        /// <value><c>true</c> if the initializer has run; otherwise, <c>false</c>.</value>
        public bool HasInitialized { get { return syncObject == null; } }

        /// <summary>
        /// Ensures that the initializer has run. This will run the
        /// initializer if it has not run before and do nothing otherwise.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the initializer was run just now; otherwise, <c>false</c>.
        /// </returns>
        public bool Initialize()
        {
            var localSyncObj = syncObject;
            if (localSyncObj == null)
                return false;

            bool result = false;
            lock (localSyncObj)
            {
                if (init != null)
                {
                    var f = init;
                    init = null;
                    f();
                    result = true;
                }
            }
            syncObject = null;
            return result;
        }
    }
}

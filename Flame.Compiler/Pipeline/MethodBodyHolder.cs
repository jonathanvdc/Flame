using System;
using System.Collections.Concurrent;
using System.Threading;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Pipeline
{
    /// <summary>
    /// A container for a method body as it is being optimized.
    /// </summary>
    internal sealed class MethodBodyHolder : IDisposable
    {
        /// <summary>
        /// Creates a method body holder from an initial method body.
        /// </summary>
        /// <param name="initialBody">An initial method body.</param>
        public MethodBodyHolder(MethodBody initialBody)
        {
            this.readerWriterLock = new ReaderWriterLockSlim();
            this.currentBody = initialBody;
            this.genericInstanceCache = new ConcurrentDictionary<IMethod, MethodBody>();
        }

        private ReaderWriterLockSlim readerWriterLock;

        private MethodBody currentBody;

        private ConcurrentDictionary<IMethod, MethodBody> genericInstanceCache;

        /// <summary>
        /// Gets or sets the method body.
        /// </summary>
        /// <returns>The method body.</returns>
        public MethodBody Body
        {
            get
            {
                MethodBody result;
                try
                {
                    readerWriterLock.EnterReadLock();
                    result = currentBody;
                }
                finally
                {
                    readerWriterLock.ExitReadLock();
                }
                return result;
            }
            set
            {
                try
                {
                    readerWriterLock.EnterWriteLock();
                    currentBody = value;
                }
                finally
                {
                    readerWriterLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets the method body for a specialization of the method to
        /// which this method body holder belongs.
        /// </summary>
        /// <param name="method">A method specialization.</param>
        /// <returns>The method body for the specialization.</returns>
        public MethodBody GetSpecializationBody(IMethod method)
        {
            if (method.GetRecursiveGenericDeclaration() == method)
            {
                return Body;
            }
            else
            {
                MethodBody result;
                try
                {
                    readerWriterLock.EnterReadLock();
                    result = genericInstanceCache.GetOrAdd(method, GetActualSpecializationBody);
                }
                finally
                {
                    readerWriterLock.ExitReadLock();
                }
                return result;
            }
        }

        private MethodBody GetActualSpecializationBody(IMethod method)
        {
            var mapping = method.GetRecursiveGenericArgumentMapping();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disposes of this method body holder.
        /// </summary>
        public void Dispose()
        {
            readerWriterLock.Dispose();
            currentBody = null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Flame.Binding;
using Flame.Primitives;

namespace Flame.Front.Target
{
    /// <summary>
    /// An environment that uses a binder to map names to types. This binder can be
    /// configured separately to get around the fact that the environment is created
    /// before dependencies are resolved and assemblies are compiled.
    /// </summary>
    public sealed class StandaloneEnvironment : IEnvironment
    {
        public StandaloneEnvironment()
            : this(new EmptyBinder())
        { }

        public StandaloneEnvironment(IBinder Binder)
        {
            Configure(Binder);
        }

        private IBinder binder;
        private Dictionary<IType, IType> equivTypeDict;
        private Dictionary<IType, IType> builtinTypeDict;
        private ReaderWriterLockSlim equivTypeLock;
        private IType rootTypeVal;
        private IType enumerableTypeVal;
        private IType enumeratorTypeVal;

        /// <summary>
        /// Configures this environment with the given binder.
        /// </summary>
        /// <param name="Binder">The binder to configure this environment with.</param>
        public void Configure(IBinder Binder)
        {
            this.binder = Binder;
            this.rootTypeVal = null;
            this.enumerableTypeVal = null;
            this.enumeratorTypeVal = null;
            this.equivTypeDict = new Dictionary<IType, IType>();
            this.builtinTypeDict = new Dictionary<IType, IType>();
            this.equivTypeLock = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// The environment identifier for the standalone environment.
        /// </summary>
        public const string StandaloneIdentifier = "standalone";

        /// <inheritdoc/>
        public IType RootType
        {
            get
            {
                if (rootTypeVal == null)
                {
                    rootTypeVal = binder.BindType(objectName);
                }
                return rootTypeVal;
            }
        }

        /// <inheritdoc/>
        public IType EnumerableType
        {
            get
            {
                if (enumerableTypeVal == null)
                {
                    enumerableTypeVal = binder.BindType(enumerableName);
                }
                return enumerableTypeVal;
            }
        }

        /// <inheritdoc/>
        public IType EnumeratorType
        {
            get
            {
                if (enumeratorTypeVal == null)
                {
                    enumeratorTypeVal = binder.BindType(enumeratorName);
                }
                return enumeratorTypeVal;
            }
        }

        /// <inheritdoc/>
        public string Name => StandaloneIdentifier;

        /// <inheritdoc/>
        public IType GetEquivalentType(IType Type)
        {
            // This method basically implements a bidirectional concurrent dictionary.
            // It relies on the fact that `binder.BindType(Type)` always returns the
            // same result when given the same argument.
            //
            // The process works like so:
            //
            //     1. Exclude non-primitive types with a quick check.
            //
            //     2. Try to get a cached version of the equivalent type.
            //        If we find one, then we return immediately. Multiple
            //        calls to `GetEquivalentType` can perform this step
            //        simultaneously.
            //
            //     3. Find the equivalent type for the given type. Again, this
            //        step can be performed concurrently.
            //
            //     4. Update the equivalent type and builtin type dictionaries.
            //        Only one call to `GetEquivalentType` can do this at any
            //        given time.
            //
            //     5. Return the equivalent type found in step three.
            //
            // Note that a race condition may arise: two calls to `GetEquivalentType(Type)`
            // may run step three simultaneously and then step four sequentially, causing
            // the dictionaries to be updated *twice.* This is where we rely on the fact
            // that `binder.BindType(Type)` is constant: if `binder.BindType(Type)`
            // is the exact same type for both calls to `GetEquivalentType(Type)`, then
            // the second dictionary update won't change anything.

            // Non-primitive types are not mapped to equivalent types.
            if (!Type.GetIsPrimitive())
            {
                return Type;
            }

            // Try to get a cached version.
            IType equivType;
            if (TryGetCachedType(equivTypeDict, Type, out equivType))
            {
                return equivType;
            }

            equivType = binder.BindType(Type.FullName);

            if (equivType != null)
            {
                UpdateCachedEquivalentType(Type, equivType);
                return equivType;
            }
            else
            {
                return Type;
            }
        }

        private bool TryGetCachedType(Dictionary<IType, IType> Dictionary, IType Type, out IType EquivalentType)
        {
            bool result;
            try
            {
                equivTypeLock.EnterReadLock();
                result = Dictionary.TryGetValue(Type, out EquivalentType);
            }
            finally
            {
                equivTypeLock.ExitReadLock();
            }
            return result;
        }

        private void UpdateCachedEquivalentType(IType Type, IType EquivalentType)
        {
            try
            {
                equivTypeLock.EnterWriteLock();
                equivTypeDict[Type] = EquivalentType;
                builtinTypeDict[EquivalentType] = Type;
            }
            finally
            {
                equivTypeLock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IType> GetDefaultBaseTypes(IType Type, IEnumerable<IType> CurrentBaseTypes)
        {
            if (RootType == null || Type.GetIsInterface())
                return Enumerable.Empty<IType>();

            foreach (var baseTy in CurrentBaseTypes)
            {
                if (!baseTy.GetIsInterface())
                    return Enumerable.Empty<IType>();
            }

            return new IType[] { RootType };
        }

        /// <inheritdoc/>
        public IType GetBuiltinType(IType Type)
        {
            IType builtinType;
            if (TryGetCachedType(builtinTypeDict, Type, out builtinType))
            {
                return builtinType;
            }
            else
            {
                return Type;
            }
        }

        private static readonly QualifiedName objectName =
            new SimpleName("Object")
            .Qualify("System");

        private static readonly QualifiedName enumerableName =
            new SimpleName("IEnumerable", 1)
            .Qualify("Generic")
            .Qualify("Collections")
            .Qualify("System");

        private static readonly QualifiedName enumeratorName =
            new SimpleName("IEnumerator", 1)
            .Qualify("Generic")
            .Qualify("Collections")
            .Qualify("System");
    }
}
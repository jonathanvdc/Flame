using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A data structure that can be queried for method implementations.
    /// </summary>
    internal sealed class VTable
    {
        private VTable(IType type)
        {
            var impls = new Dictionary<IMethod, IMethod>();
            foreach (var baseType in type.BaseTypes)
            {
                foreach (var pair in Get(baseType).implementations)
                {
                    impls[pair.Key] = pair.Value;
                }
            }
            foreach (var method in type.Methods.Concat(
                type.Properties.SelectMany(prop => prop.Accessors)))
            {
                foreach (var baseMethod in method.BaseMethods)
                {
                    impls[baseMethod] = method;
                }
            }
            this.implementations = impls;
        }

        private Dictionary<IMethod, IMethod> implementations;

        public IMethod GetImplementation(IMethod method)
        {
            IMethod impl;
            if (implementations.TryGetValue(method, out impl))
            {
                return GetImplementation(impl);
            }
            else
            {
                return method;
            }
        }

        // This cache interns all VTables.
        private static ConditionalWeakTable<IType, VTable> instanceCache
            = new ConditionalWeakTable<IType, VTable>();

        public static VTable Get(IType type)
        {
            return instanceCache.GetValue(type, t => new VTable(t));
        }
    }
}

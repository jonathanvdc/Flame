using Flame.Compiler.Visitors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// A class that stores and manages global, type and method metadata.
    /// </summary>
    public class MetadataManager
    {
        public MetadataManager()
        {
            this.GlobalMetadata = new RandomAccessOptions();
            this.typeMetadata = new ConcurrentDictionary<IType, RandomAccessOptions>();
            this.methodMetadata = new ConcurrentDictionary<IMethod, RandomAccessOptions>();
        }

        /// <summary>
        /// Gets the global pass options.
        /// </summary>
        public RandomAccessOptions GlobalMetadata { [Pure] get; private set; }

        private ConcurrentDictionary<IType, RandomAccessOptions> typeMetadata;
        private ConcurrentDictionary<IMethod, RandomAccessOptions> methodMetadata;

        /// <summary>
        /// Gets pass options specific to the given type.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public RandomAccessOptions GetTypeMetadata(IType Type)
        {
            return typeMetadata.GetOrAdd(Type, _ => new RandomAccessOptions());
        }

        /// <summary>
        /// Gets pass options specific to the given method.
        /// </summary>
        /// <param name="Method"></param>
        /// <returns></returns>
        public RandomAccessOptions GetMethodMetadata(IMethod Method)
        {
            return methodMetadata.GetOrAdd(Method, _ => new RandomAccessOptions());
        }

        /// <summary>
        /// Gets pass metadata for the given method.
        /// </summary>
        /// <param name="Method"></param>
        /// <returns></returns>
        public PassMetadata GetPassMetadata(IMethod Method)
        {
            return new PassMetadata(GlobalMetadata,
                GetTypeMetadata(Method.DeclaringType),
                GetMethodMetadata(Method));
        }
    }
}

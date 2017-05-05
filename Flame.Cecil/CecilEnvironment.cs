using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Variables;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Flame.Cecil
{
    /// <summary>
    /// A runtime environment description for the CLR platform.
    /// </summary>
    public class CecilEnvironment : IEnvironment
    {
        public CecilEnvironment(CecilModule Module)
        {
            this.Module = Module;
            var typeSystem = Module.Module.TypeSystem;
            this.environmentEquivalents = new Dictionary<IType, IType>()
            {
                { PrimitiveTypes.Int8, Module.ConvertStrict(typeSystem.SByte) },
                { PrimitiveTypes.Int16, Module.ConvertStrict(typeSystem.Int16) },
                { PrimitiveTypes.Int32, Module.ConvertStrict(typeSystem.Int32) },
                { PrimitiveTypes.Int64, Module.ConvertStrict(typeSystem.Int64) },
                { PrimitiveTypes.UInt8, Module.ConvertStrict(typeSystem.Byte) },
                { PrimitiveTypes.UInt16, Module.ConvertStrict(typeSystem.UInt16) },
                { PrimitiveTypes.UInt32, Module.ConvertStrict(typeSystem.UInt32) },
                { PrimitiveTypes.UInt64, Module.ConvertStrict(typeSystem.UInt64) },
                { PrimitiveTypes.Float32, Module.ConvertStrict(typeSystem.Single) },
                { PrimitiveTypes.Float64, Module.ConvertStrict(typeSystem.Double) },
                { PrimitiveTypes.Char, Module.ConvertStrict(typeSystem.Char) },
                { PrimitiveTypes.Boolean, Module.ConvertStrict(typeSystem.Boolean) },
                { PrimitiveTypes.String, Module.ConvertStrict(typeSystem.String) }
            };
            this.arrayTypes = new ConcurrentDictionary<int, CecilArrayType>();
        }

        /// <summary>
        /// Gets the module with which this environment is associated.
        /// </summary>
        /// <returns>The module with which this environment is associated.</returns>
        public CecilModule Module { get; private set; }

        private Dictionary<IType, IType> environmentEquivalents;
        private ConcurrentDictionary<int, CecilArrayType> arrayTypes;

        public string Name
        {
            get { return "CLR/Cecil"; }
        }

        public IType RootType
        {
            get { return Module.TypeSystem.Object; }
        }

        public IType EnumerableType
        {
            get { return Module.TypeSystem.Enumerable; }
        }

        public IType EnumeratorType
        {
            get { return Module.TypeSystem.Enumerator; }
        }

        public IEnumerable<IType> GetDefaultBaseTypes(
            IType Type, IEnumerable<IType> DefaultBaseTypes)
        {
            if (Type.GetIsInterface())
                return Enumerable.Empty<IType>();
                
            foreach (var baseTy in DefaultBaseTypes)
            {
                if (!baseTy.GetIsInterface())
                    return Enumerable.Empty<IType>();
            }
            return new IType[] { Type.GetIsValueType() ? Module.TypeSystem.ValueType : RootType };
        }

        /// <inheritdoc/>
        public IType GetEquivalentType(IType Type)
        {
            IType equivalentType;
            if (environmentEquivalents.TryGetValue(Type, out equivalentType))
            {
                return equivalentType;
            }
            else if (Type.GetIsArray())
            {
                var arrayType = Type.AsArrayType();
                var genericCecilArrayType = arrayTypes.GetOrAdd(
                    arrayType.ArrayRank,
                    rank => new CecilArrayType(rank, Module));
                return genericCecilArrayType.MakeGenericType(new IType[] { arrayType.ElementType });
            }
            else
            {
                return Type;
            }
        }

        /// <summary>
        /// Creates a binder for this runtime environment's core type system.
        /// </summary>
        /// <returns>A binder for the core type system.</returns>
        public IBinder CreateEnvironmentBinder()
        {
            return new CecilEnvironmentBinder(this);
        }

        /// <summary>
        /// A binder for the core type system of CLR runtime environments.
        /// </summary>
        private class CecilEnvironmentBinder : IBinder
        {
            public CecilEnvironmentBinder(CecilEnvironment Environment)
            {
                this.cecilEnv = Environment;
                this.typeMap = new Dictionary<QualifiedName, IType>();
                foreach (var envType in Environment.environmentEquivalents.Values)
                {
                    this.typeMap[envType.FullName] = envType;
                }
            }

            private CecilEnvironment cecilEnv;

            private Dictionary<QualifiedName, IType> typeMap;

            /// <inheritdoc/>
            public IEnvironment Environment { get { return cecilEnv; } }

            /// <inheritdoc/>
            public IType BindType(QualifiedName Name)
            {
                IType result;
                if (typeMap.TryGetValue(Name, out result))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }

            /// <inheritdoc/>
            public IEnumerable<IType> GetTypes()
            {
                return typeMap.Values;
            }
        }
    }
}

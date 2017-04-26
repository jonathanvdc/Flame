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

namespace Flame.Cecil
{
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
        }

        public CecilModule Module { get; private set; }

        private Dictionary<IType, IType> environmentEquivalents;

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
            else
            {
                return Type;
            }
        }
    }
}

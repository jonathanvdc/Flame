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
            this.environmentEquivalents = new Dictionary<IType, IType>()
            {
                { PrimitiveTypes.Int8, Module.ConvertStrict(Module.Module.TypeSystem.SByte) },
                { PrimitiveTypes.Int16, Module.ConvertStrict(Module.Module.TypeSystem.Int16) },
                { PrimitiveTypes.Int32, Module.ConvertStrict(Module.Module.TypeSystem.Int32) },
                { PrimitiveTypes.Int64, Module.ConvertStrict(Module.Module.TypeSystem.Int64) },
                { PrimitiveTypes.UInt8, Module.ConvertStrict(Module.Module.TypeSystem.Byte) },
                { PrimitiveTypes.UInt16, Module.ConvertStrict(Module.Module.TypeSystem.UInt16) },
                { PrimitiveTypes.UInt32, Module.ConvertStrict(Module.Module.TypeSystem.UInt32) },
                { PrimitiveTypes.UInt64, Module.ConvertStrict(Module.Module.TypeSystem.UInt64) },
                { PrimitiveTypes.Float32, Module.ConvertStrict(Module.Module.TypeSystem.Single) },
                { PrimitiveTypes.Float64, Module.ConvertStrict(Module.Module.TypeSystem.Double) },
                { PrimitiveTypes.Char, Module.ConvertStrict(Module.Module.TypeSystem.Char) },
                { PrimitiveTypes.Boolean, Module.ConvertStrict(Module.Module.TypeSystem.Boolean) },
                { PrimitiveTypes.String, Module.ConvertStrict(Module.Module.TypeSystem.String) }
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

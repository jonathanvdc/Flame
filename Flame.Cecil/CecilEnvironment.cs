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
        }

        public CecilModule Module { get; private set; }

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
    }
}

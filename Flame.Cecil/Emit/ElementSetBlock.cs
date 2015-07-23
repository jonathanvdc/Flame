using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ElementSetBlock : ICecilBlock
    {
        public ElementSetBlock(ILElementVariable Variable, ICecilBlock Value)
        {
            this.Variable = Variable;
            this.Value = Value;
        }

        public ILElementVariable Variable { get; private set; }
        public ICecilBlock Value { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Variable.Container.Emit(Context);
            var containerType = Context.Stack.Pop().AsContainerType();
            var elemType = containerType.GetElementType();
            int rank;
            if (containerType.get_IsVector())
            {
                rank = containerType.AsVectorType().GetDimensions().Length;
            }
            else
            {
                rank = containerType.AsArrayType().ArrayRank;
            }
            var variableArgs = Variable.Arguments;
            IType[] argumentTypes = new IType[variableArgs.Length + 1];
            for (int i = 0; i < variableArgs.Length; i++)
            {
                variableArgs[i].Emit(Context);
                argumentTypes[i] = Context.Stack.Pop();
            }
            Value.Emit(Context);
            Context.Stack.Pop();
            argumentTypes[argumentTypes.Length - 1] = elemType;
            if (rank == 1)
            {
                new ElementSetEmitter().Emit(Context, elemType);
            }
            else
            {
                var getMethod = containerType.GetMethod("Set", false, elemType, argumentTypes);
                Context.Emit(OpCodes.Call, getMethod);
            }
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Variable.CodeGenerator; }
        }
    }
}

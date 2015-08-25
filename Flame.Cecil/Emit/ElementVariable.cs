using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ElementVariable : UnmanagedVariableBase
    {
        public ElementVariable(ICodeGenerator CodeGenerator, ICecilBlock Container, ICecilBlock[] Arguments)
            : base(CodeGenerator)
        {
            this.Container = Container;
            this.Arguments = Arguments;
        }

        public ICecilBlock Container { get; private set; }
        public ICecilBlock[] Arguments { get; private set; }

        public IContainerType ContainerType
        {
            get { return Container.BlockType.AsContainerType(); }
        }

        public override IType Type
        {
            get { return ContainerType.GetElementType(); }
        }

        private Tuple<IContainerType, IType, IType[]> EmitContainerAndArguments(IEmitContext Context)
        {
            Container.Emit(Context);
            var containerType = Context.Stack.Pop().AsContainerType();
            var elementType = containerType.GetElementType();
            var argumentTypes = new IType[Arguments.Length];
            for (int i = 0; i < Arguments.Length; i++)
            {
                Arguments[i].Emit(Context);
                argumentTypes[i] = Context.Stack.Pop();
            }
            return Tuple.Create(containerType, elementType, argumentTypes);
        }

        public override void EmitAddress(IEmitContext Context)
        {
            var contAndArgTypes = EmitContainerAndArguments(Context);

            var containerType = contAndArgTypes.Item1;
            var elemType = contAndArgTypes.Item2;
            var argumentTypes = contAndArgTypes.Item3;
            int rank = argumentTypes.Length;

            if (rank == 1)
            {
                Context.Emit(OpCodes.Ldelema, elemType);
            }
            else
            {
                var getMethod = containerType.GetMethod("Address", false, elemType, argumentTypes);
                Context.Emit(OpCodes.Call, getMethod);
            }
        }

        public override void EmitLoad(IEmitContext Context)
        {
            var contAndArgTypes = EmitContainerAndArguments(Context);

            var containerType = contAndArgTypes.Item1;
            var elemType = contAndArgTypes.Item2;
            var argumentTypes = contAndArgTypes.Item3;
            int rank = argumentTypes.Length;

            if (rank == 1)
            {
                new ElementGetEmitter().Emit(Context, elemType);
            }
            else
            {
                var getMethod = containerType.GetMethod("Get", false, elemType, argumentTypes);
                Context.Emit(OpCodes.Call, getMethod);
            }
        }

        public override void EmitStore(IEmitContext Context, ICecilBlock Value)
        {
            var contAndArgTypes = EmitContainerAndArguments(Context);

            var containerType = contAndArgTypes.Item1;
            var elemType = contAndArgTypes.Item2;
            var argumentTypes = contAndArgTypes.Item3.Concat(new IType[] { elemType }).ToArray();
            int rank = contAndArgTypes.Item3.Length;

            Value.Emit(Context);
            Context.Stack.Pop();
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

        public override void EmitRelease(IEmitContext Context)
        {
            // Do nothing.
        }
    }
}

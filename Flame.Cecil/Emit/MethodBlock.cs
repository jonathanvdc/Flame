using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class MethodBlock : ICecilBlock
    {
        public MethodBlock(ICodeGenerator CodeGenerator, IMethod Method, ICecilBlock Caller)
        {
            System.Diagnostics.Debug.Assert(CodeGenerator != null);
            System.Diagnostics.Debug.Assert(Method != null);
            this.CodeGenerator = CodeGenerator;
            this.Method = Method;
            this.Caller = Caller;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IMethod Method { get; private set; }
        public ICecilBlock Caller { get; private set; }

        public static void EmitCaller(ICecilBlock Caller, IMethod Target, IEmitContext Context)
        {
            Caller.Emit(Context);
            var type = Context.Stack.Peek();
            if (!type.get_IsPointer() && ILCodeGenerator.IsPossibleValueType(type)) // Sometimes the address of a value type has to be taken
            {
                Context.EmitPushPointerCommands((IUnmanagedCodeGenerator)Caller.CodeGenerator, type, true);
            }
        }

        public void Emit(IEmitContext Context)
        {
            if (Caller == null)
            {
                Context.Emit(OpCodes.Ldnull);
            }
            else
            {
                EmitCaller(Caller, Method, Context);
                Context.Stack.Pop();
            }
            // Push a function pointer on the stack.
            if (ILCodeGenerator.UseVirtualCall(Method))
            {
                Context.Emit(OpCodes.Ldvirtftn, Method);
            }
            else
            {
                Context.Emit(OpCodes.Ldftn, Method);
            }

            var resultType = MethodType.Create(Method);
            var module = CodeGenerator.GetModule();
            var importedType = module.TypeSystem.GetCanonicalDelegate(Method);

            Context.Emit(OpCodes.Newobj, importedType.GetConstructors().Single());
            
            Context.Stack.Push(resultType);
        }

        public IStackBehavior StackBehavior
        {
            get 
            {
                return new SinglePushBehavior(MethodType.Create(Method));
            }
        }
    }
}

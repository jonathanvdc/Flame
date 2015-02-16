using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class InvocationInstruction : ILInstruction
    {
        public InvocationInstruction(ICodeGenerator CodeGenerator, IMethod Method, IInstruction Caller, IInstruction[] Arguments)
            : base(CodeGenerator)
        {
            this.Method = Method;
            this.Caller = Caller;
            this.Arguments = Arguments;
        }

        public IInstruction Caller { get; private set; }
        public IInstruction[] Arguments { get; private set; }
        public IMethod Method { get; private set; }

        #region EmitPushCaller

        protected IInstruction EmitPushCallerPointer()
        {
            return new PushPointerInstruction(CodeGenerator);
        }

        /// <summary>
        /// Pushes the caller of a method on the stack, and returns a boolean value that indicates if the method should be prefixed by a constrained opcode.
        /// </summary>
        /// <param name="Function"></param>
        /// <param name="Caller"></param>
        /// <returns></returns>
        protected bool EmitPushCaller(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            var callerType = TypeStack.Pop();
            if (callerType.get_IsPointer())
            {
                return true;
            }
            else if (callerType.get_IsValueType())
            {
                // for methods that are declared within a value type
                if (Method.DeclaringType.get_IsValueType())
                {
                    EmitPushCallerPointer().Emit(Context, TypeStack);
                    return false;
                }
                else if (Method.get_IsVirtual())
                {
                    EmitPushCallerPointer().Emit(Context, TypeStack);
                    return true;
                }
                else
                {
                    Context.Emit(OpCodes.Box, callerType);
                    return false;
                }
            }
            else if ((Method.get_IsVirtual() || Method.DeclaringType.get_IsInterface()) && callerType.get_IsGenericParameter())
            {
                EmitPushCallerPointer().Emit(Context, TypeStack);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        public static void EmitArguments(ICodeGenerator CodeGenerator, ICommandEmitContext Context, Stack<IType> TypeStack, IMethod Method, IInstruction[] Arguments)
        {
            var paramTypes = Method.GetParameters().GetTypes();
            for (int i = 0; i < Arguments.Length; i++)
            {
                var item = Arguments[i];
                var desiredType = paramTypes[i];
                item.Emit(Context, TypeStack);
                if (TypeStack.Peek() != paramTypes[i])
                {
                    TypeCastInstruction convExpr = new TypeCastInstruction(CodeGenerator, new EmptyInstruction(CodeGenerator), desiredType);
                    convExpr.Emit(Context, TypeStack);
                }
                TypeStack.Pop();
            }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Caller.Emit(Context, TypeStack);
            var callerType = TypeStack.Peek();
            bool constrained = EmitPushCaller(Context, TypeStack);

            EmitArguments(CodeGenerator, Context, TypeStack, Method, Arguments);

            bool virtCall = (Method.get_IsVirtual() || Method.get_IsAbstract() || Method.DeclaringType.get_IsInterface()) && !Method.DeclaringType.get_IsValueType();
            if (virtCall)
            {
                if (constrained)
                {
                    Context.Emit(OpCodes.Constrained, callerType);
                }
                Context.Emit(OpCodes.CallVirtual, Method);
            }
            else
            {
                Context.Emit(OpCodes.Call, Method);
            }
            TypeStack.Push(Method.ReturnType);
        }
    }
}

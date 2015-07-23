using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class DefaultValueBlock : ICecilBlock
    {
        public DefaultValueBlock(ICodeGenerator CodeGenerator, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type { get; private set; }

        public void Emit(IEmitContext Context)
        {
            if (Type.get_IsInteger() || Type.get_IsBit() || Type.Equals(PrimitiveTypes.Char) || Type.Equals(PrimitiveTypes.Boolean))
            {
                int magnitude = Type.GetPrimitiveMagnitude();
                switch (magnitude)
                {
                    case 4:
                        Context.Emit(OpCodes.Ldc_I8, (long)0);
                        break;
                    default:
                        Context.Emit(OpCodes.Ldc_I4_0);
                        break;
                }
            } 
            else if (Type.Equals(PrimitiveTypes.Float32))
            {
                Context.Emit(OpCodes.Ldc_R4, (float)0);
            } 
            else if (Type.Equals(PrimitiveTypes.Float64))
            {
                Context.Emit(OpCodes.Ldc_R8, (double)0);
            }
            else
            {
                bool useGenericApproach;
                if (Type.get_IsValueType())
                {
                    useGenericApproach = true;
                }
                else if (Type.get_IsGenericParameter())
                {
                    useGenericApproach = !(Type as IGenericParameter).get_IsReferenceType();
                }
                else
                {
                    useGenericApproach = false;
                }
                if (useGenericApproach) // requires an extra variable, but works every time
                {
                    var variable = ((IUnmanagedCodeGenerator)CodeGenerator).DeclareUnmanagedVariable(Type);

                    ((ICecilBlock)variable.EmitAddressOf()).Emit(Context);
                    Context.Stack.Pop();
                    Context.Emit(OpCodes.Initobj, Type);
                    ((ICecilBlock)variable.EmitGet()).Emit(Context);
                    Context.Stack.Pop();
                    ((ICecilBlock)variable.EmitRelease()).Emit(Context);
                }
                else
                {
                    Context.Emit(OpCodes.Ldnull);
                }
            }
            Context.Stack.Push(Type);
        }

        public IType BlockType
        {
            get { return Type; }
        }
    }
}

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

        /// <summary>
        /// Tells if default-value blocks will initialize the given type with an `initobj` sequence.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static bool RequiresInitobj(IType Type)
        {
            return Type.GetIsValueType() || (Type.GetIsGenericParameter() && !Type.GetIsReferenceType());
        }

        /// <summary>
        /// Tells if `initobj Type` is shorter than statically determining the
        /// default value of the given type, and then assigning that to
        /// a pointer's backing value.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static bool PreferInitobj(IType Type)
        {
            return RequiresInitobj(Type) || Type.GetPrimitiveMagnitude() > 3 || Type.GetIsFloatingPoint();
        }

        public void Emit(IEmitContext Context)
        {
            if (Type.GetIsInteger() || Type.GetIsBit() || Type.Equals(PrimitiveTypes.Char) || Type.Equals(PrimitiveTypes.Boolean))
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
            else if (RequiresInitobj(Type)) // requires an extra variable, but works every time
            {
                var variable = ((IUnmanagedCodeGenerator)CodeGenerator).DeclareUnmanagedLocal(new UniqueTag(), Type);

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
            Context.Stack.Push(Type);
        }

        public IType BlockType
        {
            get { return Type; }
        }
    }
}

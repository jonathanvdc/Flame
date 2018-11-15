using System;
using System.Collections.Generic;
using Flame.Compiler.Instructions;
using Flame.TypeSystem;

namespace Flame.Compiler
{
    public partial struct Instruction
    {
        /// <summary>
        /// Creates an instruction that allocates storage on the stack
        /// for a variable number of elements of a particular type.
        /// </summary>
        /// <param name="elementType">
        /// The type of value to allocate storage for.
        /// </param>
        /// <returns>
        /// An alloca-array instruction.
        /// </returns>
        public static Instruction CreateAllocaArray(
            IType elementType, ValueTag elementCount)
        {
            return AllocaArrayPrototype.Create(elementType).Instantiate(elementCount);
        }

        /// <summary>
        /// Creates an instruction that allocates storage on the stack
        /// for a single value element of a particular type.
        /// </summary>
        /// <param name="elementType">
        /// The type of value to allocate storage for.
        /// </param>
        /// <returns>
        /// An alloca instruction.
        /// </returns>
        public static Instruction CreateAlloca(IType elementType)
        {
            return AllocaPrototype.Create(elementType).Instantiate();
        }

        /// <summary>
        /// Creates an instruction that boxes a value type,
        /// turning it into a reference type (aka box pointer).
        /// </summary>
        /// <param name="elementType">
        /// The type of value to box.
        /// </param>
        /// <param name="element">
        /// The value to box.
        /// </param>
        /// <returns>
        /// A box instruction.
        /// </returns>
        public static Instruction CreateBox(IType elementType, ValueTag element)
        {
            return BoxPrototype.Create(elementType).Instantiate(element);
        }

        /// <summary>
        /// Creates an instruction that calls a particular method.
        /// </summary>
        /// <param name="callee">The method to call.</param>
        /// <param name="lookup">
        /// The method implementation lookup technique to use for calling the method.
        /// </param>
        /// <param name="arguments">
        /// The extended argument list: a list of arguments prefixed with a 'this'
        /// argument, if applicable.
        /// </param>
        /// <returns>
        /// A call instruction.
        /// </returns>
        public static Instruction CreateCall(
            IMethod callee,
            MethodLookup lookup,
            IReadOnlyList<ValueTag> arguments)
        {
            return CallPrototype.Create(callee, lookup).Instantiate(arguments);
        }

        /// <summary>
        /// Creates an instruction that calls a particular method.
        /// </summary>
        /// <param name="callee">The method to call.</param>
        /// <param name="lookup">
        /// The method implementation lookup technique to use for calling the method.
        /// </param>
        /// <param name="thisArgument">
        /// The 'this' argument for the method call.
        /// </param>
        /// <param name="arguments">
        /// The argument list for the method call.
        /// </param>
        /// <returns>
        /// A call instruction.
        /// </returns>
        public static Instruction CreateCall(
            IMethod callee,
            MethodLookup lookup,
            ValueTag thisArgument,
            IReadOnlyList<ValueTag> arguments)
        {
            return CallPrototype.Create(callee, lookup)
                .Instantiate(thisArgument, arguments);
        }

        /// <summary>
        /// Creates an instruction that creates a constant value of a
        /// particular type.
        /// </summary>
        /// <param name="value">
        /// The constant value to produce.
        /// </param>
        /// <param name="type">
        /// The type of value created by the instruction.
        /// </param>
        /// <returns>
        /// A constant instruction.
        /// </returns>
        public static Instruction CreateConstant(Constant value, IType type)
        {
            return ConstantPrototype.Create(value, type).Instantiate();
        }

        /// <summary>
        /// Creates a copy instruction, which creates an alias for
        /// an existing value.
        /// </summary>
        /// <param name="type">
        /// The type of value to copy.
        /// </param>
        /// <param name="value">
        /// The value to copy.
        /// </param>
        /// <returns>
        /// A copy instruction.
        /// </returns>
        public static Instruction CreateCopy(IType type, ValueTag value)
        {
            return CopyPrototype.Create(type).Instantiate(value);
        }

        /// <summary>
        /// Creates an indirect call instruction.
        /// </summary>
        /// <param name="returnType">
        /// The type of value returned by the callee.
        /// </param>
        /// <param name="parameterTypes">
        /// A list of parameter types.
        /// </param>
        /// <param name="callee">
        /// The delegate or function pointer to call.
        /// </param>
        /// <param name="arguments">
        /// The argument list for the call.
        /// </param>
        /// <returns>
        /// An indirect call instruction.
        /// </returns>
        public static Instruction CreateIndirectCall(
            IType returnType,
            IReadOnlyList<IType> parameterTypes,
            ValueTag callee,
            IReadOnlyList<ValueTag> arguments)
        {
            return IndirectCallPrototype.Create(returnType, parameterTypes)
                .Instantiate(callee, arguments);
        }

        /// <summary>
        /// Creates a load instruction.
        /// </summary>
        /// <param name="pointeeType">The type of value to load.</param>
        /// <param name="pointer">A pointer to the value to load.</param>
        /// <returns>A load instruction.</returns>
        public static Instruction CreateLoad(
            IType pointeeType, ValueTag pointer)
        {
            return LoadPrototype.Create(pointeeType).Instantiate(pointer);
        }

        /// <summary>
        /// Creates a new-delegate instruction.
        /// </summary>
        /// <param name="delegateType">
        /// The type of the resulting delegate or function pointer.
        /// </param>
        /// <param name="callee">
        /// The method called by the resulting delegate or function
        /// pointer.
        /// </param>
        /// <param name="thisArgument">
        /// The 'this' argument, if any. A <c>null</c> value means that
        /// there is no 'this' argument.
        /// </param>
        /// <param name="lookup">
        /// The method implementation lookup technique to use.
        /// </param>
        /// <returns>
        /// A new-delegate instruction.
        /// </returns>
        public static Instruction CreateNewDelegate(
            IType delegateType,
            IMethod callee,
            ValueTag thisArgument,
            MethodLookup lookup)
        {
            return NewDelegatePrototype
                .Create(delegateType, callee, thisArgument != null, lookup)
                .Instantiate(thisArgument);
        }

        /// <summary>
        /// Creates a new-object instruction that allocates storage on the
        /// heap for an object and initializes it using a constructor.
        /// </summary>
        /// <param name="constructor">
        /// The constructor to initialize objects with.
        /// </param>
        /// <param name="arguments">
        /// A list of arguments to call the constructor with.
        /// </param>
        /// <returns>
        /// A new-object instruction.
        /// </returns>
        public static Instruction CreateNewObject(
            IMethod constructor,
            IReadOnlyList<ValueTag> arguments)
        {
            return NewObjectPrototype.Create(constructor).Instantiate(arguments);
        }

        /// <summary>
        /// Creates a reinterpret-cast instruction that converts
        /// from one pointer type to another.
        /// </summary>
        /// <param name="targetType">
        /// A type to convert operands to.
        /// </param>
        /// <param name="operand">
        /// An operand to convert to the target type.
        /// </param>
        /// <returns>
        /// A reinterpret-cast instruction.
        /// </returns>
        public static Instruction CreateReinterpretCast(
            PointerType targetType, ValueTag operand)
        {
            return ReinterpretCastPrototype.Create(targetType).Instantiate(operand);
        }

        /// <summary>
        /// Creates a store instruction.
        /// </summary>
        /// <param name="pointeeType">
        /// The type of <paramref name="value"/>. Also the type of
        /// value pointed to by <paramref name="pointer"/>.
        /// </param>
        /// <param name="pointer">
        /// A pointer the target of the store.
        /// </param>
        /// <param name="value">
        /// A value to store at <paramref name="pointer"/>'s pointee.
        /// </param>
        /// <returns>
        /// A store instruction.
        /// </returns>
        public static Instruction CreateStore(
            IType pointeeType,
            ValueTag pointer,
            ValueTag value)
        {
            return StorePrototype.Create(pointeeType).Instantiate(pointer, value);
        }

        /// <summary>
        /// Creates an arithmetic intrinsic.
        /// </summary>
        /// <param name="operatorName">
        /// The name of the arithmetic operator to apply.
        /// </param>
        /// <param name="resultType">
        /// The type of value produced by the intrinsic.
        /// </param>
        /// <param name="parameterTypes">
        /// The parameter types taken by the intrinsic.
        /// </param>
        /// <param name="arguments">
        /// The argument list to pass to the instruction.
        /// </param>
        /// <returns>
        /// An arithmetic intrinsic.
        /// </returns>
        public static Instruction CreateArithmeticIntrinsic(
            string operatorName,
            IType resultType,
            IReadOnlyList<IType> parameterTypes,
            IReadOnlyList<ValueTag> arguments)
        {
            return ArithmeticIntrinsics.CreatePrototype(
                operatorName,
                resultType,
                parameterTypes).Instantiate(arguments);
        }

        /// <summary>
        /// Creates a binary arithmetic intrinsic that is closed
        /// under the type of its arguments.
        /// </summary>
        /// <param name="operatorName">
        /// The name of the binary arithmetic operator to apply.
        /// </param>
        /// <param name="elementType">
        /// The type of both parameter types and the result type
        /// of the intrinsic.
        /// </param>
        /// <param name="left">
        /// The first argument to the intrinsic.
        /// </param>
        /// <param name="right">
        /// The second argument to the intrinsic.
        /// </param>
        /// <returns>
        /// An arithmetic intrinsic.
        /// </returns>
        public static Instruction CreateBinaryArithmeticIntrinsic(
            string operatorName,
            IType elementType,
            ValueTag left,
            ValueTag right)
        {
            return CreateArithmeticIntrinsic(
                operatorName,
                elementType,
                new[] { elementType, elementType },
                new[] { left, right });
        }

        /// <summary>
        /// Creates a binary arithmetic intrinsic that produces
        /// a Boolean value.
        /// </summary>
        /// <param name="operatorName">
        /// The name of the binary arithmetic operator to apply.
        /// </param>
        /// <param name="booleanType">
        /// The type of value returned by the relational operator.
        /// </param>
        /// <param name="elementType">
        /// The type of both parameter types.
        /// </param>
        /// <param name="left">
        /// The first argument to the intrinsic.
        /// </param>
        /// <param name="right">
        /// The second argument to the intrinsic.
        /// </param>
        /// <returns>
        /// A relational arithmetic intrinsic.
        /// </returns>
        public static Instruction CreateRelationalIntrinsic(
            string operatorName,
            IType booleanType,
            IType elementType,
            ValueTag left,
            ValueTag right)
        {
            return CreateArithmeticIntrinsic(
                operatorName,
                booleanType,
                new[] { elementType, elementType },
                new[] { left, right });
        }

        /// <summary>
        /// Creates an intrinsic that converts one primitive
        /// type to another.
        /// </summary>
        /// <param name="targetType">
        /// The target primitive type: the type to convert
        /// the value to.
        /// </param>
        /// <param name="sourceType">
        /// The source primitive type: the type of the value
        /// to convert.
        /// </param>
        /// <param name="value">The value to convert.</param>
        /// <returns>A conversion instruction.</returns>
        public static Instruction CreateConvertIntrinsic(
            IType targetType,
            IType sourceType,
            ValueTag value)
        {
            return CreateArithmeticIntrinsic(
                ArithmeticIntrinsics.Operators.Convert,
                targetType,
                new[] { sourceType },
                new[] { value });
        }

        /// <summary>
        /// Creates an 'unbox_any' intrinsic.
        /// Its return type can either be a value type or a
        /// reference type (aka box pointer).
        /// If its return type is set to a value type, 'unbox_any'
        /// unboxes its argument and loads it.
        /// If 'unbox_any's return value is set to a reference type,
        /// 'unbox_any' checks that its argument is a subtype of the
        /// return type.
        /// </summary>
        /// <param name="targetType">
        /// The target type: the type to unbox or cast a box pointer to.
        /// </param>
        /// <param name="sourceType">
        /// The source type: the type of the value to convert.
        /// </param>
        /// <param name="value">The value to convert.</param>
        /// <returns>A conversion instruction.</returns>
        public static Instruction CreateUnboxAnyIntrinsic(
            IType targetType,
            IType sourceType,
            ValueTag value)
        {
            return ObjectIntrinsics.CreateUnboxAnyPrototype(targetType, sourceType)
                .Instantiate(new[] { value });
        }
    }
}

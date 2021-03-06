using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Instructions;
using Flame.Compiler.Instructions.Fused;
using Flame.TypeSystem;
using Loyc.Syntax;
using Pixie.Markup;

namespace Flame.Ir
{
    /// <summary>
    /// Defines codec elements for instruction prototypes.
    /// </summary>
    public static class InstructionCodecElements
    {
        /// <summary>
        /// A codec element for alloca-array instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<AllocaArrayPrototype, IReadOnlyList<LNode>> AllocaArray =
            new CodecElement<AllocaArrayPrototype, IReadOnlyList<LNode>>(
                "alloca_array", EncodeAllocaArray, DecodeAllocaArray);

        private static AllocaArrayPrototype DecodeAllocaArray(IReadOnlyList<LNode> data, DecoderState state)
        {
            return AllocaArrayPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeAllocaArray(AllocaArrayPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ResultType) };
        }

        /// <summary>
        /// A codec element for alloca instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<AllocaPrototype, IReadOnlyList<LNode>> Alloca =
            new CodecElement<AllocaPrototype, IReadOnlyList<LNode>>(
                "alloca", EncodeAlloca, DecodeAlloca);

        private static AllocaPrototype DecodeAlloca(IReadOnlyList<LNode> data, DecoderState state)
        {
            return AllocaPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeAlloca(AllocaPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ElementType) };
        }

        /// <summary>
        /// A codec element for box instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<BoxPrototype, IReadOnlyList<LNode>> Box =
            new CodecElement<BoxPrototype, IReadOnlyList<LNode>>(
                "box", EncodeBox, DecodeBox);

        private static BoxPrototype DecodeBox(IReadOnlyList<LNode> data, DecoderState state)
        {
            return BoxPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeBox(BoxPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ElementType) };
        }

        /// <summary>
        /// A codec element for constant instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<ConstantPrototype, IReadOnlyList<LNode>> Constant =
            new CodecElement<ConstantPrototype, IReadOnlyList<LNode>>(
                "const", EncodeConstant, DecodeConstant);

        private static ConstantPrototype DecodeConstant(IReadOnlyList<LNode> data, DecoderState state)
        {
            return ConstantPrototype.Create(state.DecodeConstant(data[0]), state.DecodeType(data[1]));
        }

        private static IReadOnlyList<LNode> EncodeConstant(ConstantPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.Value), state.Encode(value.ResultType) };
        }

        /// <summary>
        /// A codec element for call instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<CallPrototype, IReadOnlyList<LNode>> Call =
            new CodecElement<CallPrototype, IReadOnlyList<LNode>>(
                "call", EncodeCall, DecodeCall);

        private static CallPrototype DecodeCall(IReadOnlyList<LNode> data, DecoderState state)
        {
            return CallPrototype.Create(
                state.DecodeMethod(data[0]),
                state.DecodeMethodLookup(data[1]));
        }

        private static IReadOnlyList<LNode> EncodeCall(CallPrototype value, EncoderState state)
        {
            return new LNode[]
            {
                state.Encode(value.Callee),
                state.Encode(value.Lookup)
            };
        }

        /// <summary>
        /// A codec element for constrained call instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<ConstrainedCallPrototype, IReadOnlyList<LNode>> ConstrainedCall =
            new CodecElement<ConstrainedCallPrototype, IReadOnlyList<LNode>>(
                "constrained_call", EncodeConstrainedCall, DecodeConstrainedCall);

        private static ConstrainedCallPrototype DecodeConstrainedCall(IReadOnlyList<LNode> data, DecoderState state)
        {
            return ConstrainedCallPrototype.Create(
                state.DecodeMethod(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeConstrainedCall(ConstrainedCallPrototype value, EncoderState state)
        {
            return new LNode[]
            {
                state.Encode(value.Callee)
            };
        }

        /// <summary>
        /// A codec element for copy instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<CopyPrototype, IReadOnlyList<LNode>> Copy =
            new CodecElement<CopyPrototype, IReadOnlyList<LNode>>(
                "copy", EncodeCopy, DecodeCopy);

        private static CopyPrototype DecodeCopy(IReadOnlyList<LNode> data, DecoderState state)
        {
            return CopyPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeCopy(CopyPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ResultType) };
        }

        /// <summary>
        /// A codec element for dynamic cast instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<DynamicCastPrototype, IReadOnlyList<LNode>> DynamicCast =
            new CodecElement<DynamicCastPrototype, IReadOnlyList<LNode>>(
                "dynamic_cast", EncodeDynamicCast, DecodeDynamicCast);

        private static DynamicCastPrototype DecodeDynamicCast(IReadOnlyList<LNode> data, DecoderState state)
        {
            var targetType = state.DecodeType(data[0]);
            if (targetType is PointerType)
            {
                return DynamicCastPrototype.Create((PointerType)targetType);
            }
            else
            {
                state.Log.LogSyntaxError(
                    data[0],
                    new Text("expected a pointer type."));
                return null;
            }
        }

        private static IReadOnlyList<LNode> EncodeDynamicCast(DynamicCastPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.TargetType) };
        }

        /// <summary>
        /// A codec element for get-field-pointer instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<GetFieldPointerPrototype, IReadOnlyList<LNode>> GetFieldPointer =
            new CodecElement<GetFieldPointerPrototype, IReadOnlyList<LNode>>(
                "get_field_pointer", EncodeGetFieldPointer, DecodeGetFieldPointer);

        private static GetFieldPointerPrototype DecodeGetFieldPointer(IReadOnlyList<LNode> data, DecoderState state)
        {
            return GetFieldPointerPrototype.Create(state.DecodeField(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeGetFieldPointer(GetFieldPointerPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.Field) };
        }

        /// <summary>
        /// A codec element for get-static-field-pointer instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<GetStaticFieldPointerPrototype, IReadOnlyList<LNode>> GetStaticFieldPointer =
            new CodecElement<GetStaticFieldPointerPrototype, IReadOnlyList<LNode>>(
                "get_static_field_pointer", EncodeGetStaticFieldPointer, DecodeGetStaticFieldPointer);

        private static GetStaticFieldPointerPrototype DecodeGetStaticFieldPointer(IReadOnlyList<LNode> data, DecoderState state)
        {
            return GetStaticFieldPointerPrototype.Create(state.DecodeField(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeGetStaticFieldPointer(GetStaticFieldPointerPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.Field) };
        }

        /// <summary>
        /// A codec element for indirect call instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<IndirectCallPrototype, IReadOnlyList<LNode>> IndirectCall =
            new CodecElement<IndirectCallPrototype, IReadOnlyList<LNode>>(
                "indirect_call", EncodeIndirectCall, DecodeIndirectCall);

        private static IndirectCallPrototype DecodeIndirectCall(IReadOnlyList<LNode> data, DecoderState state)
        {
            return IndirectCallPrototype.Create(
                state.DecodeType(data[0]),
                data[1].Args.EagerSelect<LNode, IType>(state.DecodeType));
        }

        private static IReadOnlyList<LNode> EncodeIndirectCall(IndirectCallPrototype value, EncoderState state)
        {
            var paramTypeNodes = new List<LNode>();
            foreach (var paramType in value.ParameterTypes)
            {
                paramTypeNodes.Add(state.Encode(paramType));
            }

            return new LNode[]
            {
                state.Encode(value.ResultType),
                state.Factory.List(paramTypeNodes)
            };
        }

        /// <summary>
        /// A codec element for intrinsic instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<IntrinsicPrototype, IReadOnlyList<LNode>> Intrinsic =
            new CodecElement<IntrinsicPrototype, IReadOnlyList<LNode>>(
                "intrinsic", EncodeIntrinsic, DecodeIntrinsic);

        private static IntrinsicPrototype DecodeIntrinsic(IReadOnlyList<LNode> data, DecoderState state)
        {
            // TODO: decode exception specifications.
            return IntrinsicPrototype.Create(
                FeedbackHelpers.AssertIsId(data[0], state.Log)
                    ? data[0].Name.Name
                    : "error",
                state.DecodeType(data[1]),
                data[2].Args.EagerSelect<LNode, IType>(state.DecodeType));
        }

        private static IReadOnlyList<LNode> EncodeIntrinsic(IntrinsicPrototype value, EncoderState state)
        {
            // TODO: encode exception specifications.

            var paramTypeNodes = new List<LNode>();
            foreach (var paramType in value.ParameterTypes)
            {
                paramTypeNodes.Add(state.Encode(paramType));
            }

            return new LNode[]
            {
                state.Factory.Id(value.Name),
                state.Encode(value.ResultType),
                state.Factory.List(paramTypeNodes)
            };
        }

        /// <summary>
        /// A codec element for load instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<LoadPrototype, IReadOnlyList<LNode>> Load =
            new CodecElement<LoadPrototype, IReadOnlyList<LNode>>(
                "load", EncodeLoad, DecodeLoad);

        private static LoadPrototype DecodeLoad(IReadOnlyList<LNode> data, DecoderState state)
        {
            if (data.Count >= 3)
            {
                return LoadPrototype.Create(
                    state.DecodeType(data[0]),
                    state.DecodeBoolean(data[1]),
                    state.DecodeAlignment(data[2]));
            }
            else if (data.Count >= 2)
            {
                return LoadPrototype.Create(
                    state.DecodeType(data[0]),
                    state.DecodeBoolean(data[1]));
            }
            else
            {
                return LoadPrototype.Create(state.DecodeType(data[0]));
            }
        }

        private static IReadOnlyList<LNode> EncodeLoad(LoadPrototype value, EncoderState state)
        {
            if (!value.Alignment.IsNaturallyAligned)
            {
                return new LNode[]
                {
                    state.Encode(value.ResultType),
                    state.Encode(value.IsVolatile),
                    state.Encode(value.Alignment)
                };
            }
            else if (value.IsVolatile)
            {
                return new LNode[]
                {
                    state.Encode(value.ResultType),
                    state.Encode(value.IsVolatile)
                };
            }
            else
            {
                return new LNode[] { state.Encode(value.ResultType) };
            }
        }

        /// <summary>
        /// A codec element for fused load-field instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<LoadFieldPrototype, IReadOnlyList<LNode>> LoadField =
            new CodecElement<LoadFieldPrototype, IReadOnlyList<LNode>>(
                "load_field", EncodeLoadField, DecodeLoadField);

        private static LoadFieldPrototype DecodeLoadField(IReadOnlyList<LNode> data, DecoderState state)
        {
            return LoadFieldPrototype.Create(state.DecodeField(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeLoadField(LoadFieldPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.Field) };
        }

        /// <summary>
        /// A codec element for new-delegate instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<NewDelegatePrototype, IReadOnlyList<LNode>> NewDelegate =
            new CodecElement<NewDelegatePrototype, IReadOnlyList<LNode>>(
                "new_delegate", EncodeNewDelegate, DecodeNewDelegate);

        private static NewDelegatePrototype DecodeNewDelegate(IReadOnlyList<LNode> data, DecoderState state)
        {
            return NewDelegatePrototype.Create(
                state.DecodeType(data[0]),
                state.DecodeMethod(data[1]),
                state.DecodeBoolean(data[2]),
                state.DecodeMethodLookup(data[3]));
        }

        private static IReadOnlyList<LNode> EncodeNewDelegate(NewDelegatePrototype value, EncoderState state)
        {
            return new LNode[]
            {
                state.Encode(value.ResultType),
                state.Encode(value.Callee),
                state.Encode(value.HasThisArgument),
                state.Encode(value.Lookup)
            };
        }


        /// <summary>
        /// A codec element for new-delegate instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<NewObjectPrototype, IReadOnlyList<LNode>> NewObject =
            new CodecElement<NewObjectPrototype, IReadOnlyList<LNode>>(
                "new_object", EncodeNewObject, DecodeNewObject);

        private static NewObjectPrototype DecodeNewObject(IReadOnlyList<LNode> data, DecoderState state)
        {
            return NewObjectPrototype.Create(state.DecodeMethod(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeNewObject(NewObjectPrototype value, EncoderState state)
        {
            return new LNode[]
            {
                state.Encode(value.Constructor)
            };
        }


        /// <summary>
        /// A codec element for reinterpret cast instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<ReinterpretCastPrototype, IReadOnlyList<LNode>> ReinterpretCast =
            new CodecElement<ReinterpretCastPrototype, IReadOnlyList<LNode>>(
                "reinterpret_cast", EncodeReinterpretCast, DecodeReinterpretCast);

        private static ReinterpretCastPrototype DecodeReinterpretCast(IReadOnlyList<LNode> data, DecoderState state)
        {
            var targetType = state.DecodeType(data[0]);
            if (targetType is PointerType)
            {
                return ReinterpretCastPrototype.Create((PointerType)targetType);
            }
            else
            {
                state.Log.LogSyntaxError(
                    data[0],
                    new Text("expected a pointer type."));
                return null;
            }
        }

        private static IReadOnlyList<LNode> EncodeReinterpretCast(ReinterpretCastPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.TargetType) };
        }

        /// <summary>
        /// A codec element for store instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<StorePrototype, IReadOnlyList<LNode>> Store =
            new CodecElement<StorePrototype, IReadOnlyList<LNode>>(
                "store", EncodeStore, DecodeStore);

        private static StorePrototype DecodeStore(IReadOnlyList<LNode> data, DecoderState state)
        {
            if (data.Count >= 3)
            {
                return StorePrototype.Create(
                    state.DecodeType(data[0]),
                    state.DecodeBoolean(data[1]),
                    state.DecodeAlignment(data[2]));
            }
            else if (data.Count >= 2)
            {
                return StorePrototype.Create(
                    state.DecodeType(data[0]),
                    state.DecodeBoolean(data[1]));
            }
            else
            {
                return StorePrototype.Create(state.DecodeType(data[0]));
            }
        }

        private static IReadOnlyList<LNode> EncodeStore(StorePrototype value, EncoderState state)
        {
            if (!value.Alignment.IsNaturallyAligned)
            {
                return new LNode[]
                {
                    state.Encode(value.ResultType),
                    state.Encode(value.IsVolatile),
                    state.Encode(value.Alignment)
                };
            }
            else if (value.IsVolatile)
            {
                return new LNode[]
                {
                    state.Encode(value.ResultType),
                    state.Encode(value.IsVolatile)
                };
            }
            else
            {
                return new LNode[] { state.Encode(value.ResultType) };
            }
        }

        /// <summary>
        /// A codec element for fused store-field instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<StoreFieldPrototype, IReadOnlyList<LNode>> StoreField =
            new CodecElement<StoreFieldPrototype, IReadOnlyList<LNode>>(
                "store_field", EncodeStoreField, DecodeStoreField);

        private static StoreFieldPrototype DecodeStoreField(IReadOnlyList<LNode> data, DecoderState state)
        {
            return StoreFieldPrototype.Create(state.DecodeField(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeStoreField(StoreFieldPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.Field) };
        }

        /// <summary>
        /// A codec element for unbox instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<UnboxPrototype, IReadOnlyList<LNode>> Unbox =
            new CodecElement<UnboxPrototype, IReadOnlyList<LNode>>(
                "unbox", EncodeUnbox, DecodeUnbox);

        private static UnboxPrototype DecodeUnbox(IReadOnlyList<LNode> data, DecoderState state)
        {
            return UnboxPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeUnbox(UnboxPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ElementType) };
        }

        /// <summary>
        /// A codec element for sizeof instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<SizeOfPrototype, IReadOnlyList<LNode>> SizeOf =
            new CodecElement<SizeOfPrototype, IReadOnlyList<LNode>>(
                "sizeof", EncodeSizeOf, DecodeSizeOf);

        private static SizeOfPrototype DecodeSizeOf(IReadOnlyList<LNode> data, DecoderState state)
        {
            return SizeOfPrototype.Create(state.DecodeType(data[0]), state.DecodeType(data[1]));
        }

        private static IReadOnlyList<LNode> EncodeSizeOf(SizeOfPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.MeasuredType), state.Encode(value.ResultType) };
        }

        /// <summary>
        /// Gets a codec that contains all sub-codecs defined in this class.
        /// </summary>
        /// <returns>A codec.</returns>
        public static Codec<InstructionPrototype, LNode> All
        {
            get
            {
                return new PiecewiseCodec<InstructionPrototype>()
                    .Add(AllocaArray)
                    .Add(Alloca)
                    .Add(Box)
                    .Add(Call)
                    .Add(Constant)
                    .Add(ConstrainedCall)
                    .Add(Copy)
                    .Add(DynamicCast)
                    .Add(GetFieldPointer)
                    .Add(GetStaticFieldPointer)
                    .Add(IndirectCall)
                    .Add(Intrinsic)
                    .Add(Load)
                    .Add(LoadField)
                    .Add(NewDelegate)
                    .Add(NewObject)
                    .Add(ReinterpretCast)
                    .Add(Store)
                    .Add(StoreField)
                    .Add(Unbox)
                    .Add(SizeOf);
            }
        }
    }
}

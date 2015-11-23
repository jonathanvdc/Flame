using Flame.Compiler;
using Flame.MIPS.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public static class AssemblerExtensions
    {
        public static int GetSize(this IType Type)
        {
            if (Type is IAssemblerType)
            {
                return ((IAssemblerType)Type).GetSize();
            }
            else if (Type.get_IsPrimitive())
            {
                return Type.GetPrimitiveSize();
            }
            else if (Type.get_IsPointer())
            {
                return 4;
            }
            else if (Type.get_IsVector())
            {
                var vectType = Type.AsContainerType().AsVectorType();
                return vectType.ElementType.GetSize() * vectType.Dimensions.Aggregate(1, (a, b) => a * b);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static int GetTotalSize(this IEnumerable<IType> Types)
        {
            return Types.Aggregate(0, (left, type) => left + type.GetSize());
        }

        public static IRegister EmitToRegister(this IAssemblerBlock Block, IAssemblerEmitContext Context)
        {
            var leftResult = Block.Emit(Context).Single();
            if (leftResult is IRegister)
            {
                return (IRegister)leftResult;
            }
            else
            {
                var lReg = Context.AllocateRegister(Block.Type);
                leftResult.EmitLoad(lReg).Emit(Context);
                leftResult.EmitRelease().Emit(Context);
                return lReg;
            }
        }

        /// <summary>
        /// Emits the given block, and spills it if the result is a register.
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public static IStorageLocation EmitAndSpill(this IAssemblerBlock Block, IAssemblerEmitContext Context)
        {
            return Block.Emit(Context).Single().SpillRegister(Context);
        }

        public static void EmitStoreTo(this IAssemblerBlock Block, IStorageLocation Target, IAssemblerEmitContext Context)
        {
            var leftResult = Block.Emit(Context).Single();
            leftResult.ReleaseTo(Target, Context);
        }

        public static void ReleaseTo(this IStorageLocation Location, IStorageLocation Target, IAssemblerEmitContext Context)
        {
            if (Target is IRegister)
            {
                Location.EmitLoad((IRegister)Target).Emit(Context);
                Location.EmitRelease().Emit(Context);
            }
            else
            {
                var lReg = Location.ReleaseToRegister(Context);
                Target.EmitStore(lReg).Emit(Context);
                lReg.EmitRelease().Emit(Context);
            }
        }

        public static IRegister ReleaseToRegister(this IStorageLocation StorageLocation, IAssemblerEmitContext Context)
        {
            if (StorageLocation is IRegister)
            {
                return (IRegister)StorageLocation;
            }
            else
            {
                var lReg = Context.AllocateRegister(StorageLocation.Type);
                StorageLocation.EmitLoad(lReg).Emit(Context);
                StorageLocation.EmitRelease().Emit(Context);
                return lReg;
            }
        }

        public static IRegister ReleaseToTemporaryRegister(this IStorageLocation StorageLocation, IAssemblerEmitContext Context)
        {
            if (StorageLocation is IRegister && ((IRegister)StorageLocation).IsTemporary)
            {
                return (IRegister)StorageLocation;
            }
            else
            {
                var lReg = Context.AllocateRegister(StorageLocation.Type);
                StorageLocation.EmitLoad(lReg).Emit(Context);
                StorageLocation.EmitRelease().Emit(Context);
                return lReg;
            }
        }

        /// <summary>
        /// Spills the storage location if it is a register. Otherwise, does nothing.
        /// </summary>
        /// <param name="StorageLocation"></param>
        /// <returns></returns>
        public static IStorageLocation SpillRegister(this IStorageLocation StorageLocation, IAssemblerEmitContext Context)
        {
            if (StorageLocation is IRegister)
            {
                return Context.Spill((IRegister)StorageLocation);
            }
            else
            {
                return StorageLocation;
            }
        }

        /// <summary>
        /// Turns a potentially immutable storage location into a mutable storage location.
        /// </summary>
        /// <param name="StorageLocation"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public static IStorageLocation ToMutable(this IStorageLocation StorageLocation, IAssemblerEmitContext Context)
        {
            if (StorageLocation is IConstantStorage && !((IConstantStorage)StorageLocation).IsMutable)
            {
                var register = Context.AllocateRegister(StorageLocation.Type);
                StorageLocation.EmitLoad(register).Emit(Context);
                return Context.Spill(register);
            }
            else
            {
                return StorageLocation;
            }
        }

        public static string GetRegisterName(this RegisterType Type, int Index)
        {
            switch (Type)
            {
                case RegisterType.ReturnValue:
                    return "$v" + Index;
                case RegisterType.Temporary:
                    return "$t" + Index;
                case RegisterType.StackPointer:
                    return "$sp";
                case RegisterType.FramePointer:
                    return "$fp";
                case RegisterType.AddressRegister:
                    return "$ra";
                case RegisterType.Local:
                    return "$s" + Index;
                case RegisterType.Argument:
                    return "$a" + Index;
                case RegisterType.FloatRegister:
                    return "$f" + Index;
                case RegisterType.Zero:
                    return "$zero";
                case RegisterType.AssemblerTemporary:
                    return "$at";
                case RegisterType.GlobalPointer:
                    return "$gp";
                default:
                    throw new NotSupportedException("Register type '" + Type + "' is not supported.");
            }
        }

        public static void EmitEmptyLine(this IAssemblerEmitContext Context)
        {
            Context.EmitComment(null);
        }

        public static IRegister GetRegister(this IAssemblerEmitContext Context, RegisterType Kind, int Index, IType Type)
        {
            return Context.GetRegister(Kind, Index, Type, false);
        }
        public static IRegister AcquireRegister(this IAssemblerEmitContext Context, RegisterType Kind, int Index, IType Type)
        {
            return Context.GetRegister(Kind, Index, Type, true);
        }

        public static bool CanAcquire(this RegisterType Kind)
        {
            switch (Kind)
            {
                case RegisterType.ReturnValue:
                case RegisterType.Temporary:
                case RegisterType.Local:
                case RegisterType.Argument:
                case RegisterType.FloatRegister:
                    return true;

                case RegisterType.StackPointer:
                case RegisterType.FramePointer:
                case RegisterType.AddressRegister:
                case RegisterType.Zero:
                case RegisterType.AssemblerTemporary:
                case RegisterType.GlobalPointer:
                default:
                    return false;
            }
        }

        public static IStorageLocation ToStorageLocation(this IStaticDataItem DataItem, ICodeGenerator CodeGenerator)
        {
            return new StaticStorage(CodeGenerator, DataItem);
        }

        public static IFlowControlStructure GetFlowControl(this IAssemblerEmitContext Context, UniqueTag Tag)
        {
            foreach (var item in Context.FlowControl)
            {
                if (item.Tag == Tag)
                {
                    return item;
                }
            }
            return null;
        }
    }
}

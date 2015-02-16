using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public static class OpCodes
    {
        #region Flow Operations

        public const byte NopOpCode = 0x00;
        public static OpCode Nop
        {
            get
            {
                return OpCode.DefineOpCode(NopOpCode);
            }
        }
        public const byte BreakOpCode = 0x01;
        public static OpCode Break
        {
            get
            {
                return OpCode.DefineOpCode(BreakOpCode);
            }
        }
        public const byte ReturnOpCode = 0x2A;
        public static OpCode Return
        {
            get
            {
                return OpCode.DefineOpCode(ReturnOpCode);
            }
        }

        #endregion

        #region Stack Operations

        public const byte PopOpCode = 0x26;
        public static OpCode Pop
        {
            get
            {
                return OpCode.DefineOpCode(PopOpCode);
            }
        }
        public const byte DuplicateOpCode = 0x25;
        public static OpCode Duplicate
        {
            get
            {
                return OpCode.DefineOpCode(DuplicateOpCode);
            }
        }

        #endregion

        #region Conversions

        #region Pointers

        public const byte ConvertPointerOpCode = 0xD3;
        public static OpCode ConvertPointer
        {
            get
            {
                return OpCode.DefineOpCode(ConvertPointerOpCode);
            }
        }

        public const byte ConvertUnsignedPointerOpCode = 0xE0;
        public static OpCode ConvertUnsignedPointer
        {
            get
            {
                return OpCode.DefineOpCode(ConvertUnsignedPointerOpCode);
            }
        }

        #endregion

        #region Signed Integers

        public const byte ConvertInt8OpCode = 0x67;
        public static OpCode ConvertInt8
        {
            get
            {
                return OpCode.DefineOpCode(ConvertInt8OpCode);
            }
        }
        public const byte ConvertInt16OpCode = 0x68;
        public static OpCode ConvertInt16
        {
            get
            {
                return OpCode.DefineOpCode(ConvertInt16OpCode);
            }
        }
        public const byte ConvertInt32OpCode = 0x69;
        public static OpCode ConvertInt32
        {
            get
            {
                return OpCode.DefineOpCode(ConvertInt32OpCode);
            }
        }
        public const byte ConvertInt64OpCode = 0x6A;
        public static OpCode ConvertInt64
        {
            get
            {
                return OpCode.DefineOpCode(ConvertInt64OpCode);
            }
        }

        #endregion

        #region Floats

        public const byte ConvertFloat32OpCode = 0x6B;
        public static OpCode ConvertFloat32
        {
            get
            {
                return OpCode.DefineOpCode(ConvertFloat32OpCode);
            }
        }
        public const byte ConvertFloat64OpCode = 0x6C;
        public static OpCode ConvertFloat64
        {
            get
            {
                return OpCode.DefineOpCode(ConvertFloat64OpCode);
            }
        }

        #endregion

        #endregion

        #region Constants

        public const byte LoadNullOpCode = 0x14;
        public static OpCode LoadNull
        {
            get
            {
                return OpCode.DefineOpCode(LoadNullOpCode);
            }
        }

        #region Int32

        public const byte LoadInt32OpCode = 0x20;
        public static OpCode LoadInt32
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32OpCode, 4);
            }
        }
        public const byte LoadInt32_0OpCode = 0x16;
        public static OpCode LoadInt32_0
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32_0OpCode);
            }
        }
        public const byte LoadInt32_1OpCode = 0x17;
        public static OpCode LoadInt32_1
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32_1OpCode);
            }
        }
        public const byte LoadInt32_2OpCode = 0x18;
        public static OpCode LoadInt32_2
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32_2OpCode);
            }
        }
        public const byte LoadInt32_3OpCode = 0x19;
        public static OpCode LoadInt32_3
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32_3OpCode);
            }
        }
        public const byte LoadInt32_4OpCode = 0x1A;
        public static OpCode LoadInt32_4
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32_4OpCode);
            }
        }
        public const byte LoadInt32_5OpCode = 0x1B;
        public static OpCode LoadInt32_5
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32_5OpCode);
            }
        }
        public const byte LoadInt32_6OpCode = 0x1C;
        public static OpCode LoadInt32_6
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32_6OpCode);
            }
        }
        public const byte LoadInt32_7OpCode = 0x1D;
        public static OpCode LoadInt32_7
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32_7OpCode);
            }
        }
        public const byte LoadInt32_8OpCode = 0x1E;
        public static OpCode LoadInt32_8
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32_8OpCode);
            }
        }
        public const byte LoadInt32_M1OpCode = 0x15;
        public static OpCode LoadInt32_M1
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32_M1OpCode);
            }
        }
        public const byte LoadInt32ShortOpCode = 0x1F;
        public static OpCode LoadInt32Short
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32ShortOpCode, 1);
            }
        }

        #endregion

        public const byte LoadInt64OpCode = 0x21;
        public static OpCode LoadInt64
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt64OpCode, 8);
            }
        }
        public const byte LoadFloat32OpCode = 0x22;
        public static OpCode LoadFloat32
        {
            get
            {
                return OpCode.DefineOpCode(LoadFloat32OpCode, 4);
            }
        }
        public const byte LoadFloat64OpCode = 0x23;
        public static OpCode LoadFloat64
        {
            get
            {
                return OpCode.DefineOpCode(LoadFloat64OpCode, 8);
            }
        }

        #region LoadString

        public const byte LoadStringOpCode = 0x72;
        public static OpCode LoadString
        {
            get
            {
                return OpCode.DefineOpCode(LoadStringOpCode, 4);
            }
        }

        #endregion

        #region LoadToken

        public const byte LoadTokenOpCode = 0xD0;
        public static OpCode LoadToken
        {
            get
            {
                return OpCode.DefineOpCode(LoadTokenOpCode, 4);
            }
        }

        #endregion

        #endregion

        #region Pointers

        #region LoadAddress

        public const byte LoadAddressPointerOpCode = 0x4D;
        public static OpCode LoadAddressPointer
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressPointerOpCode);
            }
        }

        public const byte LoadAddressReferenceOpCode = 0x50;
        public static OpCode LoadAddressReference
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressReferenceOpCode);
            }
        }

        #region Signed Integers

        public const byte LoadAddressInt8OpCode = 0x46;
        public static OpCode LoadAddressInt8
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressInt8OpCode);
            }
        }
        public const byte LoadAddressInt16OpCode = 0x48;
        public static OpCode LoadAddressInt16
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressInt16OpCode);
            }
        }
        public const byte LoadAddressInt32OpCode = 0x4A;
        public static OpCode LoadAddressInt32
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressInt32OpCode);
            }
        }
        public const byte LoadAddressInt64OpCode = 0x4C;
        public static OpCode LoadAddressInt64
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressInt64OpCode);
            }
        }

        #endregion

        #region Unsigned Integers

        public const byte LoadAddressUInt8OpCode = 0x47;
        public static OpCode LoadAddressUInt8
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressUInt8OpCode);
            }
        }
        public const byte LoadAddressUInt16OpCode = 0x49;
        public static OpCode LoadAddressUInt16
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressUInt16OpCode);
            }
        }
        public const byte LoadAddressUInt32OpCode = 0x4B;
        public static OpCode LoadAddressUInt32
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressUInt32OpCode);
            }
        }

        #endregion

        #region Floats

        public const byte LoadAddressFloat32OpCode = 0x4E;
        public static OpCode LoadAddressFloat32
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressFloat32OpCode);
            }
        }

        public const byte LoadAddressFloat64OpCode = 0x4F;
        public static OpCode LoadAddressFloat64
        {
            get
            {
                return OpCode.DefineOpCode(LoadAddressFloat64OpCode);
            }
        }

        #endregion

        #endregion

        #region StoreAddress

        public const byte StoreAddressPointerOpCode = 0xDF;
        public static OpCode StoreAddressPointer
        {
            get
            {
                return OpCode.DefineOpCode(StoreAddressPointerOpCode);
            }
        }

        public const byte StoreAddressReferenceOpCode = 0x51;
        public static OpCode StoreAddressReference
        {
            get
            {
                return OpCode.DefineOpCode(StoreAddressReferenceOpCode);
            }
        }

        #region Signed Integers

        public const byte StoreAddressInt8OpCode = 0x52;
        public static OpCode StoreAddressInt8
        {
            get
            {
                return OpCode.DefineOpCode(StoreAddressInt8OpCode);
            }
        }
        public const byte StoreAddressInt16OpCode = 0x53;
        public static OpCode StoreAddressInt16
        {
            get
            {
                return OpCode.DefineOpCode(StoreAddressInt16OpCode);
            }
        }
        public const byte StoreAddressInt32OpCode = 0x54;
        public static OpCode StoreAddressInt32
        {
            get
            {
                return OpCode.DefineOpCode(StoreAddressInt32OpCode);
            }
        }
        public const byte StoreAddressInt64OpCode = 0x55;
        public static OpCode StoreAddressInt64
        {
            get
            {
                return OpCode.DefineOpCode(StoreAddressInt64OpCode);
            }
        }

        #endregion

        #region Floats

        public const byte StoreAddressFloat32OpCode = 0x56;
        public static OpCode StoreAddressFloat32
        {
            get
            {
                return OpCode.DefineOpCode(StoreAddressFloat32OpCode);
            }
        }
        public const byte StoreAddressFloat64OpCode = 0x57;
        public static OpCode StoreAddressFloat64
        {
            get
            {
                return OpCode.DefineOpCode(StoreAddressFloat64OpCode);
            }
        }

        #endregion

        #endregion

        #region LoadObject/StoreObject

        public const byte LoadObjectOpCode = 0x71;
        public static OpCode LoadObject
        {
            get
            {
                return OpCode.DefineOpCode(LoadObjectOpCode, 4);
            }
        }

        public const byte StoreObjectOpCode = 0x81;
        public static OpCode StoreObject
        {
            get
            {
                return OpCode.DefineOpCode(StoreObjectOpCode, 4);
            }
        }

        #endregion

        #endregion

        #region Variables

        #region Locals

        #region Load

        public const byte LoadLocalExtension = 0x0C;
        public static OpCode LoadLocal
        {
            get
            {
                return OpCode.DefineExtendedOpCode(LoadLocalExtension, 2);
            }
        }
        public const byte LoadLocal_0OpCode = 0x06;
        public static OpCode LoadLocal_0
        {
            get
            {
                return OpCode.DefineOpCode(LoadLocal_0OpCode);
            }
        }
        public const byte LoadLocal_1OpCode = 0x07;
        public static OpCode LoadLocal_1
        {
            get
            {
                return OpCode.DefineOpCode(LoadLocal_1OpCode);
            }
        }
        public const byte LoadLocal_2OpCode = 0x08;
        public static OpCode LoadLocal_2
        {
            get
            {
                return OpCode.DefineOpCode(LoadLocal_2OpCode);
            }
        }
        public const byte LoadLocal_3OpCode = 0x09;
        public static OpCode LoadLocal_3
        {
            get
            {
                return OpCode.DefineOpCode(LoadLocal_3OpCode);
            }
        }
        public const byte LoadLocalShortOpCode = 0x11;
        public static OpCode LoadLocalShort
        {
            get
            {
                return OpCode.DefineOpCode(LoadLocalShortOpCode, 1);
            }
        }

        #endregion

        #region Load Address

        public const byte LoadLocalAddressExtension = 0x0D;
        public static OpCode LoadLocalAddress
        {
            get
            {
                return OpCode.DefineExtendedOpCode(LoadLocalAddressExtension, 2);
            }
        }
        public const byte LoadLocalAddressShortOpCode = 0x12;
        public static OpCode LoadLocalAddressShort
        {
            get
            {
                return OpCode.DefineOpCode(LoadLocalAddressShortOpCode, 1);
            }
        }

        #endregion

        #region Store

        public const byte StoreLocalExtension = 0x0E;
        public static OpCode StoreLocal
        {
            get
            {
                return OpCode.DefineExtendedOpCode(StoreLocalExtension, 2);
            }

        }
        public const byte StoreLocal_0OpCode = 0x0A;
        public static OpCode StoreLocal_0
        {
            get
            {
                return OpCode.DefineOpCode(StoreLocal_0OpCode);
            }
        }
        public const byte StoreLocal_1OpCode = 0x0B;
        public static OpCode StoreLocal_1
        {
            get
            {
                return OpCode.DefineOpCode(StoreLocal_1OpCode);
            }
        }
        public const byte StoreLocal_2OpCode = 0x0C;
        public static OpCode StoreLocal_2
        {
            get
            {
                return OpCode.DefineOpCode(StoreLocal_2OpCode);
            }
        }
        public const byte StoreLocal_3OpCode = 0x0D;
        public static OpCode StoreLocal_3
        {
            get
            {
                return OpCode.DefineOpCode(StoreLocal_3OpCode);
            }
        }
        public const byte StoreLocalShortOpCode = 0x13;
        public static OpCode StoreLocalShort
        {
            get
            {
                return OpCode.DefineOpCode(StoreLocalShortOpCode, 1);
            }
        }

        #endregion

        #endregion

        #region Arguments

        #region Load

        public const byte LoadArgumentExtension = 0x09;
        public static OpCode LoadArgument
        {
            get
            {
                return OpCode.DefineExtendedOpCode(LoadArgumentExtension, 2);
            }
        }
        public const byte LoadArgument_0OpCode = 0x02;
        public static OpCode LoadArgument_0
        {
            get
            {
                return OpCode.DefineOpCode(LoadArgument_0OpCode);
            }
        }
        public const byte LoadArgument_1OpCode = 0x03;
        public static OpCode LoadArgument_1
        {
            get
            {
                return OpCode.DefineOpCode(LoadArgument_1OpCode);
            }
        }
        public const byte LoadArgument_2OpCode = 0x04;
        public static OpCode LoadArgument_2
        {
            get
            {
                return OpCode.DefineOpCode(LoadArgument_2OpCode);
            }
        }
        public const byte LoadArgument_3OpCode = 0x05;
        public static OpCode LoadArgument_3
        {
            get
            {
                return OpCode.DefineOpCode(LoadArgument_3OpCode);
            }
        }
        public const byte LoadArgumentShortOpCode = 0x0E;
        public static OpCode LoadArgumentShort
        {
            get
            {
                return OpCode.DefineOpCode(LoadArgumentShortOpCode, 1);
            }
        }

        public const byte LoadArgumentAddressExtension = 0x0A;
        public static OpCode LoadArgumentAddress
        {
            get
            {
                return OpCode.DefineExtendedOpCode(LoadArgumentAddressExtension, 2);
            }
        }
        public const byte LoadArgumentAddressShortOpCode = 0x0F;
        public static OpCode LoadArgumentAddressShort
        {
            get
            {
                return OpCode.DefineOpCode(LoadArgumentAddressShortOpCode, 1);
            }
        }

        #endregion

        #region Store

        public const byte StoreArgumentExtension = 0x0B;
        public static OpCode StoreArgument
        {
            get
            {
                return OpCode.DefineExtendedOpCode(0x0B, 2);
            }
        }
        public const byte StoreArgumentShortOpCode = 0x10;
        public static OpCode StoreArgumentShort
        {
            get
            {
                return OpCode.DefineOpCode(0x10, 1);
            }
        }

        #endregion

        #endregion

        #endregion

        #region Branching

        public const byte BranchOpCode = 0x38;
        public static OpCode Branch
        {
            get
            {
                return OpCode.DefineOpCode(BranchOpCode, 4);
            }
        }
        public const byte BranchShortOpCode = 0x2B;
        public static OpCode BranchShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchShortOpCode, 1);
            }
        }
        public const byte BranchFalseOpCode = 0x39;
        public static OpCode BranchFalse
        {
            get
            {
                return OpCode.DefineOpCode(BranchFalseOpCode, 4);
            }
        }
        public const byte BranchFalseShortOpCode = 0x2C;
        public static OpCode BranchFalseShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchFalseShortOpCode, 1);
            }
        }

        #region A <= B

        public const byte BranchLessThanOrEqualsOpCode = 0x3E;
        public static OpCode BranchLessThanOrEquals
        {
            get
            {
                return OpCode.DefineOpCode(BranchLessThanOrEqualsOpCode, 4);
            }
        }
        public const byte BranchLessThanOrEqualsUnsignedOpCode = 0x43;
        public static OpCode BranchLessThanOrEqualsUnsigned
        {
            get
            {
                return OpCode.DefineOpCode(BranchLessThanOrEqualsUnsignedOpCode, 4);
            }
        }
        public const byte BranchLessThanOrEqualsShortOpCode = 0x31;
        public static OpCode BranchLessThanOrEqualsShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchLessThanOrEqualsShortOpCode, 1);
            }
        }
        public const byte BranchLessThanOrEqualsUnsignedShortOpCode = 0x36;
        public static OpCode BranchLessThanOrEqualsUnsignedShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchLessThanOrEqualsUnsignedShortOpCode, 1);
            }
        }

        #endregion

        #region A > B

        public const byte BranchGreaterThanOpCode = 0x3D;
        public static OpCode BranchGreaterThan
        {
            get
            {
                return OpCode.DefineOpCode(BranchGreaterThanOpCode, 4);
            }
        }
        public const byte BranchGreaterThanUnsignedOpCode = 0x42;
        public static OpCode BranchGreaterThanUnsigned
        {
            get
            {
                return OpCode.DefineOpCode(BranchGreaterThanUnsignedOpCode, 4);
            }
        }
        public const byte BranchGreaterThanShortOpCode = 0x30;
        public static OpCode BranchGreaterThanShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchGreaterThanShortOpCode, 1);
            }
        }
        public const byte BranchGreaterThanUnsignedShortOpCode = 0x35;
        public static OpCode BranchGreaterThanUnsignedShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchGreaterThanUnsignedShortOpCode, 1);
            }
        }

        #endregion

        #region A < B

        public const byte BranchLessThanOpCode = 0x3F;
        public static OpCode BranchLessThan
        {
            get
            {
                return OpCode.DefineOpCode(BranchLessThanOpCode, 4);
            }
        }
        public const byte BranchLessThanUnsignedOpCode = 0x44;
        public static OpCode BranchLessThanUnsigned
        {
            get
            {
                return OpCode.DefineOpCode(BranchLessThanUnsignedOpCode, 4);
            }
        }
        public const byte BranchLessThanShortOpCode = 0x32;
        public static OpCode BranchLessThanShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchLessThanShortOpCode, 1);
            }
        }
        public const byte BranchLessThanUnsignedShortOpCode = 0x37;
        public static OpCode BranchLessThanUnsignedShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchLessThanUnsignedShortOpCode, 1);
            }
        }

        #endregion

        #region A == B

        public const byte BranchEqualOpCode = 0x3B;
        public static OpCode BranchEqual
        {
            get
            {
                return OpCode.DefineOpCode(BranchEqualOpCode, 4);
            }
        }
        public const byte BranchEqualShortOpCode = 0x2E;
        public static OpCode BranchEqualShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchEqualShortOpCode, 1);
            }
        }

        #endregion

        #region A != B

        public const byte BranchUnequalOpCode = 0x40;
        public static OpCode BranchUnequal
        {
            get
            {
                return OpCode.DefineOpCode(BranchUnequalOpCode, 4);
            }
        }
        public const byte BranchUnequalShortOpCode = 0x33;
        public static OpCode BranchUnequalShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchUnequalShortOpCode, 1);
            }
        }

        #endregion

        public const byte BranchTrueOpCode = 0x3A;
        public static OpCode BranchTrue
        {
            get
            {
                return OpCode.DefineOpCode(BranchTrueOpCode, 4);
            }
        }
        public const byte BranchTrueShortOpCode = 0x2D;
        public static OpCode BranchTrueShort
        {
            get
            {
                return OpCode.DefineOpCode(BranchTrueShortOpCode, 1);
            }
        }

        #endregion

        #region Object Model

        public const byte LoadLengthOpCode = 0x8E;
        public static OpCode LoadLength
        {
            get
            {
                return OpCode.DefineOpCode(LoadLengthOpCode);
            }
        }

        public const byte CastclassOpCode = 0x74;
        public static OpCode Castclass
        {
            get
            {
                return OpCode.DefineOpCode(CastclassOpCode, 4);
            }
        }
        public const byte BoxOpCode = 0x8C;
        public static OpCode Box
        {
            get
            {
                return OpCode.DefineOpCode(BoxOpCode, 4);
            }
        }
        public const byte UnboxOpCode = 0x79;
        public static OpCode Unbox
        {
            get
            {
                return OpCode.DefineOpCode(UnboxOpCode, 4);
            }
        }
        public const byte UnboxAnyOpCode = 0xA5;
        public static OpCode UnboxAny
        {
            get
            {
                return OpCode.DefineOpCode(UnboxAnyOpCode, 4);
            }
        }

        public const byte SizeOfExtension = 0x1C;
        public static OpCode SizeOf
        {
            get
            {
                return OpCode.DefineExtendedOpCode(SizeOfExtension, 4);
            }
        }

        public const byte IsInstanceOfOpCode = 0x75;
        public static OpCode IsInstanceOf
        {
            get
            {
                return OpCode.DefineOpCode(IsInstanceOfOpCode, 4);
            }
        }

        #region Object Creation

        public const byte InitObjectExtension = 0x15;
        public static OpCode InitObject
        {
            get
            {
                return OpCode.DefineExtendedOpCode(InitObjectExtension, 4);
            }
        }

        #endregion

        #region Calls

        public const byte ConstrainedExtension = 0x16;
        public static OpCode Constrained
        {
            get
            {
                return OpCode.DefineExtendedOpCode(ConstrainedExtension, 4);
            }
        }

        public const byte CallOpCode = 0x28;
        public static OpCode Call
        {
            get
            {
                return OpCode.DefineOpCode(CallOpCode, 4);
            }
        }
        public const byte CallVirtualOpCode = 0x6F;
        public static OpCode CallVirtual
        {
            get
            {
                return OpCode.DefineOpCode(CallVirtualOpCode, 4);
            }
        }
        public const byte NewObjectOpCode = 0x73;
        public static OpCode NewObject
        {
            get
            {
                return OpCode.DefineOpCode(NewObjectOpCode, 4);
            }
        }
        public const byte NewArrayOpCode = 0x8D;
        public static OpCode NewArray
        {
            get
            {
                return OpCode.DefineOpCode(NewArrayOpCode, 4);
            }
        }

        #endregion

        #region Array Indexing

        #region Load

        public const byte LoadElementOpCode = 0xA3;
        public static OpCode LoadElement
        {
            get
            {
                return OpCode.DefineOpCode(LoadElementOpCode, 4);
            }
        }
        public const byte LoadIntElementOpCode = 0x97;
        public static OpCode LoadIntElement
        {
            get
            {
                return OpCode.DefineOpCode(LoadIntElementOpCode);
            }
        }
        public const byte LoadInt8ElementOpCode = 0x90;
        public static OpCode LoadInt8Element
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt8ElementOpCode);
            }
        }
        public const byte LoadInt16ElementOpCode = 0x92;
        public static OpCode LoadInt16Element
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt16ElementOpCode);
            }
        }
        public const byte LoadInt32ElementOpCode = 0x94;
        public static OpCode LoadInt32Element
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt32ElementOpCode);
            }
        }
        public const byte LoadInt64ElementOpCode = 0x96;
        public static OpCode LoadInt64Element
        {
            get
            {
                return OpCode.DefineOpCode(LoadInt64ElementOpCode);
            }
        }
        public const byte LoadUInt8ElementOpCode = 0x91;
        public static OpCode LoadUInt8Element
        {
            get
            {
                return OpCode.DefineOpCode(LoadUInt8ElementOpCode);
            }
        }
        public const byte LoadUInt16ElementOpCode = 0x93;
        public static OpCode LoadUInt16Element
        {
            get
            {
                return OpCode.DefineOpCode(LoadUInt16ElementOpCode);
            }
        }
        public const byte LoadUInt32ElementOpCode = 0x95;
        public static OpCode LoadUInt32Element
        {
            get
            {
                return OpCode.DefineOpCode(LoadUInt32ElementOpCode);
            }
        }
        public const byte LoadUInt64ElementOpCode = 0x97;
        public static OpCode LoadUInt64Element
        {
            get
            {
                return OpCode.DefineOpCode(LoadUInt64ElementOpCode);
            }
        }
        public const byte LoadFloat32ElementOpCode = 0x98;
        public static OpCode LoadFloat32Element
        {
            get
            {
                return OpCode.DefineOpCode(LoadFloat32ElementOpCode);
            }
        }
        public const byte LoadFloat64ElementOpCode = 0x99;
        public static OpCode LoadFloat64Element
        {
            get
            {
                return OpCode.DefineOpCode(LoadFloat64ElementOpCode);
            }
        }
        public const byte LoadReferenceElementOpCode = 0x9A;
        public static OpCode LoadReferenceElement
        {
            get
            {
                return OpCode.DefineOpCode(LoadReferenceElementOpCode);
            }
        }
        public const byte LoadElementAddressOpCode = 0x8F;
        public static OpCode LoadElementAddress
        {
            get
            {
                return OpCode.DefineOpCode(LoadElementAddressOpCode, 4);
            }
        }

        #endregion

        #region Store

        public const byte StoreElementOpCode = 0xA4;
        public static OpCode StoreElement
        {
            get
            {
                return OpCode.DefineOpCode(StoreElementOpCode, 4);
            }
        }
        public const byte StoreIntElementOpCode = 0x9B;
        public static OpCode StoreIntElement
        {
            get
            {
                return OpCode.DefineOpCode(StoreIntElementOpCode);
            }
        }
        public const byte StoreInt8ElementOpCode = 0x9C;
        public static OpCode StoreInt8Element
        {
            get
            {
                return OpCode.DefineOpCode(StoreInt8ElementOpCode);
            }
        }
        public const byte StoreInt16ElementOpCode = 0x9D;
        public static OpCode StoreInt16Element
        {
            get
            {
                return OpCode.DefineOpCode(StoreInt16ElementOpCode);
            }
        }
        public const byte StoreInt32ElementOpCode = 0x9E;
        public static OpCode StoreInt32Element
        {
            get
            {
                return OpCode.DefineOpCode(StoreInt32ElementOpCode);
            }
        }
        public const byte StoreInt64ElementOpCode = 0x9F;
        public static OpCode StoreInt64Element
        {
            get
            {
                return OpCode.DefineOpCode(StoreInt64ElementOpCode);
            }
        }
        public const byte StoreFloat32ElementOpCode = 0xA0;
        public static OpCode StoreFloat32Element
        {
            get
            {
                return OpCode.DefineOpCode(StoreFloat32ElementOpCode);
            }
        }
        public const byte StoreFloat64ElementOpCode = 0xA1;
        public static OpCode StoreFloat64Element
        {
            get
            {
                return OpCode.DefineOpCode(StoreFloat64ElementOpCode);
            }
        }
        public const byte StoreReferenceElementOpCode = 0xA2;
        public static OpCode StoreReferenceElement
        {
            get
            {
                return OpCode.DefineOpCode(StoreReferenceElementOpCode);
            }
        }

        #endregion

        #endregion

        #region Fields

        public const byte LoadFieldOpCode = 0x7B;
        public static OpCode LoadField
        {
            get
            {
                return OpCode.DefineOpCode(LoadFieldOpCode, 4);
            }
        }
        public const byte LoadStaticFieldOpCode = 0x7E;
        public static OpCode LoadStaticField
        {
            get
            {
                return OpCode.DefineOpCode(LoadStaticFieldOpCode, 4);
            }
        }

        public const byte LoadFieldAddressOpCode = 0x7C;
        public static OpCode LoadFieldAddress
        {
            get
            {
                return OpCode.DefineOpCode(LoadFieldAddressOpCode, 4);
            }
        }
        public const byte LoadStaticFieldAddressOpCode = 0x7F;
        public static OpCode LoadStaticFieldAddress
        {
            get
            {
                return OpCode.DefineOpCode(LoadStaticFieldAddressOpCode, 4);
            }
        }

        public const byte StoreFieldOpCode = 0x7D;
        public static OpCode StoreField
        {
            get
            {
                return OpCode.DefineOpCode(StoreFieldOpCode, 4);
            }
        }
        public const byte StoreStaticFieldOpCode = 0x80;
        public static OpCode StoreStaticField
        {
            get
            {
                return OpCode.DefineOpCode(StoreStaticFieldOpCode, 4);
            }
        }

        #endregion

        #endregion

        #region Math

        public const byte AddOpCode = 0x58;
        public static OpCode Add
        {
            get
            {
                return OpCode.DefineOpCode(AddOpCode);
            }
        }
        public const byte SubtractOpCode = 0x59;
        public static OpCode Subtract
        {
            get
            {
                return OpCode.DefineOpCode(SubtractOpCode);
            }
        }
        public const byte MultiplyOpCode = 0x5A;
        public static OpCode Multiply
        {
            get
            {
                return OpCode.DefineOpCode(MultiplyOpCode);
            }
        }
        public const byte DivideOpCode = 0x5B;
        public static OpCode Divide
        {
            get
            {
                return OpCode.DefineOpCode(DivideOpCode);
            }
        }

        public const byte ShiftLeftOpCode = 0x62;
        public static OpCode ShiftLeft
        {
            get
            {
                return OpCode.DefineOpCode(ShiftLeftOpCode);
            }
        }
        public const byte ShiftRightOpCode = 0x63;
        public static OpCode ShiftRight
        {
            get
            {
                return OpCode.DefineOpCode(ShiftRightOpCode);
            }
        }


        public const byte AndOpCode = 0x5F;
        public static OpCode And
        {
            get
            {
                return OpCode.DefineOpCode(AndOpCode);
            }
        }
        public const byte OrOpCode = 0x60;
        public static OpCode Or
        {
            get
            {
                return OpCode.DefineOpCode(OrOpCode);
            }
        }
        public const byte XorOpCode = 0x61;
        public static OpCode Xor
        {
            get
            {
                return OpCode.DefineOpCode(XorOpCode);
            }
        }
        public const byte NotOpCode = 0x66;
        public static OpCode Not
        {
            get
            {
                return OpCode.DefineOpCode(NotOpCode);
            }
        }
        public const byte CheckEqualsExtension = 0x01;
        public static OpCode CheckEquals
        {
            get
            {
                return OpCode.DefineExtendedOpCode(CheckEqualsExtension);
            }
        }
        public const byte CheckGreaterThanExtension = 0x02;
        public static OpCode CheckGreaterThan
        {
            get
            {
                return OpCode.DefineExtendedOpCode(CheckGreaterThanExtension);
            }
        }
        public const byte CheckGreaterThanUnsignedExtension = 0x03;
        public static OpCode CheckGreaterThanUnsigned
        {
            get
            {
                return OpCode.DefineExtendedOpCode(CheckGreaterThanUnsignedExtension);
            }
        }
        public const byte CheckLessThanExtension = 0x04;
        public static OpCode CheckLessThan
        {
            get
            {
                return OpCode.DefineExtendedOpCode(CheckLessThanExtension);
            }
        }
        public const byte CheckLessThanUnsignedExtension = 0x05;
        public static OpCode CheckLessThanUnsigned
        {
            get
            {
                return OpCode.DefineExtendedOpCode(CheckLessThanUnsignedExtension);
            }
        }

        #endregion

        #region OpCode Categories

        public static bool IsDereferencePointerOpCode(this OpCode OpCode)
        {
            switch (OpCode.Value)
            {
                case LoadObjectOpCode:
                case LoadAddressInt8OpCode:
                case LoadAddressInt16OpCode:
                case LoadAddressInt32OpCode:
                case LoadAddressInt64OpCode:
                case LoadAddressUInt8OpCode:
                case LoadAddressUInt16OpCode:
                case LoadAddressUInt32OpCode:
                case LoadAddressFloat32OpCode:
                case LoadAddressFloat64OpCode:
                case LoadAddressPointerOpCode:
                case LoadAddressReferenceOpCode:
                    return true;
                default:
                    return false;
            }
        }

        #endregion
    }
}

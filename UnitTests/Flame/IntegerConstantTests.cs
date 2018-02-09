using Flame;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Globalization;
using Flame.Constants;

namespace UnitTests
{
    [TestFixture]
    public class IntegerConstantTests
    {
        [Test]
        public void MinMax()
        {
            Assert.AreEqual(IntegerSpec.Int8.MaxValue, new BigInteger(sbyte.MaxValue));
            Assert.AreEqual(IntegerSpec.Int8.MinValue, new BigInteger(sbyte.MinValue));
            Assert.AreEqual(IntegerSpec.UInt8.MaxValue, new BigInteger(byte.MaxValue));
            Assert.AreEqual(IntegerSpec.UInt8.MinValue, new BigInteger(byte.MinValue));
            Assert.AreEqual(IntegerSpec.Int16.MaxValue, new BigInteger(short.MaxValue));
            Assert.AreEqual(IntegerSpec.Int16.MinValue, new BigInteger(short.MinValue));
            Assert.AreEqual(IntegerSpec.UInt16.MaxValue, new BigInteger(ushort.MaxValue));
            Assert.AreEqual(IntegerSpec.UInt16.MinValue, new BigInteger(ushort.MinValue));
            Assert.AreEqual(IntegerSpec.Int32.MaxValue, new BigInteger(int.MaxValue));
            Assert.AreEqual(IntegerSpec.Int32.MinValue, new BigInteger(int.MinValue));
            Assert.AreEqual(IntegerSpec.UInt32.MaxValue, new BigInteger(uint.MaxValue));
            Assert.AreEqual(IntegerSpec.UInt32.MinValue, new BigInteger(uint.MinValue));
            Assert.AreEqual(IntegerSpec.Int64.MaxValue, new BigInteger(long.MaxValue));
            Assert.AreEqual(IntegerSpec.Int64.MinValue, new BigInteger(long.MinValue));
            Assert.AreEqual(IntegerSpec.UInt64.MaxValue, new BigInteger(ulong.MaxValue));
            Assert.AreEqual(IntegerSpec.UInt64.MinValue, new BigInteger(ulong.MinValue));
        }

        [Test]
        public void Equality()
        {
            Assert.AreEqual(
                new IntegerConstant(0), 
                new IntegerConstant(0));
            Assert.AreNotEqual(
                new IntegerConstant((uint)0), 
                new IntegerConstant(0));
            Assert.AreNotEqual(
                new IntegerConstant(0), 
                new IntegerConstant((long)0));
        }

        [Test]
        public void CastSignedness()
        {
            Assert.AreEqual(
                new IntegerConstant(uint.MaxValue).CastSignedness(true), 
                new IntegerConstant(unchecked((int)uint.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(int.MinValue).CastSignedness(false), 
                new IntegerConstant(unchecked((uint)int.MinValue)));
            Assert.AreEqual(
                new IntegerConstant(ulong.MaxValue).CastSignedness(true), 
                new IntegerConstant(unchecked((long)ulong.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(long.MinValue).CastSignedness(false), 
                new IntegerConstant(unchecked((ulong)long.MinValue)));
        }

        [Test]
        public void CastSize()
        {
            Assert.AreEqual(
                new IntegerConstant(int.MaxValue).CastSize(16), 
                new IntegerConstant(unchecked((short)int.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(uint.MaxValue).CastSize(16), 
                new IntegerConstant(unchecked((ushort)uint.MaxValue)));

            Assert.AreEqual(
                new IntegerConstant(int.MaxValue).CastSize(64), 
                new IntegerConstant(unchecked((long)int.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(uint.MaxValue).CastSize(64), 
                new IntegerConstant(unchecked((ulong)uint.MaxValue)));

            Assert.AreEqual(
                new IntegerConstant(int.MinValue).CastSize(64), 
                new IntegerConstant(unchecked((long)int.MinValue)));

            for (int i = 1; i < 64; i++)
            {
                var testVals = new[] 
                { 
                    10000UL, ulong.MaxValue, 20UL, ulong.MaxValue >> 3, 
                    unchecked((ulong)long.MinValue), unchecked((ulong)long.MaxValue) 
                };
                foreach (ulong val in testVals)
                {
                    Assert.AreEqual(
                        new IntegerConstant(val).CastSize(i), 
                        new IntegerConstant(
                            new BigInteger(ToUInt(val, i)), 
                            new IntegerSpec(i, false)));
                }
            }
        }

        [Test]
        public void Cast()
        {
            Assert.AreEqual(
                new IntegerConstant(0).Cast(IntegerSpec.UInt32), 
                new IntegerConstant((uint)0));
            
            Assert.AreEqual(
                new IntegerConstant(uint.MaxValue).Cast(IntegerSpec.Int32), 
                new IntegerConstant(unchecked((int)uint.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(int.MinValue).Cast(IntegerSpec.UInt32), 
                new IntegerConstant(unchecked((uint)int.MinValue)));
            Assert.AreEqual(
                new IntegerConstant(ulong.MaxValue).Cast(IntegerSpec.Int64), 
                new IntegerConstant(unchecked((long)ulong.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(long.MinValue).Cast(IntegerSpec.UInt64), 
                new IntegerConstant(unchecked((ulong)long.MinValue)));

            Assert.AreEqual(
                new IntegerConstant(int.MaxValue).Cast(IntegerSpec.Int16), 
                new IntegerConstant(unchecked((short)int.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(uint.MaxValue).Cast(IntegerSpec.UInt16), 
                new IntegerConstant(unchecked((ushort)uint.MaxValue)));

            Assert.AreEqual(
                new IntegerConstant(int.MaxValue).Cast(IntegerSpec.Int64), 
                new IntegerConstant(unchecked((long)int.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(uint.MaxValue).Cast(IntegerSpec.UInt64), 
                new IntegerConstant(unchecked((ulong)uint.MaxValue)));

            Assert.AreEqual(
                new IntegerConstant(int.MaxValue).Cast(IntegerSpec.UInt16), 
                new IntegerConstant(unchecked((ushort)int.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(uint.MaxValue).Cast(IntegerSpec.Int16), 
                new IntegerConstant(unchecked((short)uint.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(int.MaxValue).Cast(IntegerSpec.UInt64), 
                new IntegerConstant(unchecked((ulong)int.MaxValue)));
            Assert.AreEqual(
                new IntegerConstant(uint.MaxValue).Cast(IntegerSpec.Int64), 
                new IntegerConstant(unchecked((long)uint.MaxValue)));

            Assert.AreEqual(
                new IntegerConstant(-1431655765).Cast(IntegerSpec.UInt32),
                new IntegerConstant(unchecked((uint)-1431655765)));

            foreach (var i in GetTestValues())
            {
                Assert.AreEqual(
                    new IntegerConstant((uint)i).Cast(IntegerSpec.Int32), 
                    new IntegerConstant(i));
                Assert.AreEqual(
                    new IntegerConstant(i).Cast(IntegerSpec.UInt32), 
                    new IntegerConstant(unchecked((uint)i)));
                Assert.AreEqual(
                    new IntegerConstant((ulong)(long)i).Cast(IntegerSpec.Int64), 
                    new IntegerConstant(unchecked((long)i)));
                Assert.AreEqual(
                    new IntegerConstant((long)i).Cast(IntegerSpec.UInt64), 
                    new IntegerConstant(unchecked((ulong)i)));

                Assert.AreEqual(
                    new IntegerConstant(i).Cast(IntegerSpec.Int16), 
                    new IntegerConstant(unchecked((short)i)));
                Assert.AreEqual(
                    new IntegerConstant((uint)i).Cast(IntegerSpec.UInt16), 
                    new IntegerConstant(unchecked((ushort)i)));

                Assert.AreEqual(
                    new IntegerConstant(i).Cast(IntegerSpec.Int64), 
                    new IntegerConstant(unchecked((long)i)));
                Assert.AreEqual(
                    new IntegerConstant((uint)i).Cast(IntegerSpec.UInt64), 
                    new IntegerConstant(unchecked((ulong)(uint)i)));

                Assert.AreEqual(
                    new IntegerConstant(i).Cast(IntegerSpec.UInt16), 
                    new IntegerConstant(unchecked((ushort)i)));
                Assert.AreEqual(
                    new IntegerConstant((uint)i).Cast(IntegerSpec.Int16), 
                    new IntegerConstant(unchecked((short)(uint)i)));
                Assert.AreEqual(
                    new IntegerConstant(i).Cast(IntegerSpec.UInt64), 
                    new IntegerConstant(unchecked((ulong)i)));
                Assert.AreEqual(
                    new IntegerConstant((uint)i).Cast(IntegerSpec.Int64), 
                    new IntegerConstant(unchecked((long)(uint)i)));
            }
        }

        [Test]
        public void RoundTrip()
        {
            foreach (var val in GetTestValues())
            {
                Assert.AreEqual(
                    (uint)val, new IntegerConstant((uint)val).ToUInt32());
                Assert.AreEqual(
                    (uint)val, new IntegerConstant(val).ToUInt32());
                Assert.AreEqual(
                    val, new IntegerConstant(val).ToInt32());
                Assert.AreEqual(
                    (ulong)val, new IntegerConstant((ulong)val).ToUInt64());
                Assert.AreEqual(
                    (long)val, new IntegerConstant((long)val).ToInt64());
                Assert.AreEqual(
                    (ulong)val, new IntegerConstant(val).ToUInt64());
                Assert.AreEqual(
                    (long)val, new IntegerConstant(val).ToInt64());
            }
        }

        [Test]
        public void Arithmetic()
        {
            var rand = new Random();
            TestOp((x, y) => x.Add(y), (x, y) => x + y, GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.Subtract(y), (x, y) => x - y, GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.Add(y.Negated), (x, y) => x - y, GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.Multiply(y), (x, y) => x * y, GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.Divide(y), (x, y) => x / y, GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.Remainder(y), (x, y) => x % y, GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.BitwiseAnd(y), (x, y) => x & y, GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.BitwiseOr(y), (x, y) => x | y, GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.BitwiseXor(y), (x, y) => x ^ y, GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.ShiftLeft(y.BitwiseAnd(new IntegerConstant(31))), (x, y) => x << (y & 31), GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.ShiftRight(y.BitwiseAnd(new IntegerConstant(31))), (x, y) => x >> (y & 31), GetGeneralOpTestValues(rand));
            TestOp((x, y) => x.BitwiseXor(y.OnesComplement), (x, y) => x ^ ~y, GetGeneralOpTestValues(rand));
        }

        [Test]
        public void IntegerLog()
        {
            foreach (var num in GetTestValues())
            {
                if (num >= 1)
                {
                    Assert.AreEqual(
                        new IntegerConstant(num).IntegerLog(new IntegerConstant(2)).ToInt64(),
                        (long)new IntegerConstant(num).Log(2));
                }
            }
        }

        [Test]
        public void UnsignedDivisionMagic()
        {
            // These tests are based on equivalent tests from LLVM's test suite, which can be found at
            // https://github.com/llvm-mirror/llvm/blob/master/unittests/ADT/APIntTest.cpp
            Assert.AreEqual(
                new IntegerConstant(3u).ComputeUnsignedDivisionMagic().Multiplier,
                new IntegerConstant(BigInteger.Parse("AAAAAAAB", NumberStyles.HexNumber), IntegerSpec.UInt32).Normalized);
            Assert.AreEqual(
                new IntegerConstant(3u).ComputeUnsignedDivisionMagic().ShiftAmount, 1);

            Assert.AreEqual(
                new IntegerConstant(5u).ComputeUnsignedDivisionMagic().Multiplier,
                new IntegerConstant(BigInteger.Parse("CCCCCCCD", NumberStyles.HexNumber), IntegerSpec.UInt32).Normalized);
            Assert.AreEqual(
                new IntegerConstant(5u).ComputeUnsignedDivisionMagic().ShiftAmount, 2);

            Assert.AreEqual(
                new IntegerConstant(7u).ComputeUnsignedDivisionMagic().Multiplier,
                new IntegerConstant(BigInteger.Parse("24924925", NumberStyles.HexNumber), IntegerSpec.UInt32).Normalized);
            Assert.AreEqual(
                new IntegerConstant(7u).ComputeUnsignedDivisionMagic().ShiftAmount, 3);

            Assert.AreEqual(
                new IntegerConstant(0x80000001u).ComputeUnsignedDivisionMagic().Multiplier,
                new IntegerConstant(BigInteger.Parse("FFFFFFFF", NumberStyles.HexNumber), IntegerSpec.UInt32).Normalized);
            Assert.AreEqual(
                new IntegerConstant(0x80000001u).ComputeUnsignedDivisionMagic().ShiftAmount, 31);

            Assert.AreEqual(
                new IntegerConstant(0xFFFFFFFFu).ComputeUnsignedDivisionMagic().Multiplier,
                new IntegerConstant(BigInteger.Parse("80000001", NumberStyles.HexNumber), IntegerSpec.UInt32).Normalized);
            Assert.AreEqual(
                new IntegerConstant(0xFFFFFFFFu).ComputeUnsignedDivisionMagic().ShiftAmount, 31);

            Assert.AreEqual(
                new IntegerConstant(25ul).ComputeUnsignedDivisionMagic(1).Multiplier,
                new IntegerConstant(BigInteger.Parse("A3D70A3D70A3D70B", NumberStyles.HexNumber), IntegerSpec.UInt64).Normalized);
            Assert.AreEqual(
                new IntegerConstant(25ul).ComputeUnsignedDivisionMagic(1).ShiftAmount, 4);
        }

        [Test]
        public void SignedDivisionMagic()
        {
            // These tests are based on equivalent tests from LLVM's test suite, which can be found at
            // https://github.com/llvm-mirror/llvm/blob/master/unittests/ADT/APIntTest.cpp
            Assert.AreEqual(
                new IntegerConstant(3).ComputeSignedDivisionMagic().Multiplier,
                new IntegerConstant(BigInteger.Parse("55555556", NumberStyles.HexNumber), IntegerSpec.Int32).Normalized);
            Assert.AreEqual(
                new IntegerConstant(3).ComputeSignedDivisionMagic().ShiftAmount, 0);

            Assert.AreEqual(
                new IntegerConstant(5).ComputeSignedDivisionMagic().Multiplier,
                new IntegerConstant(BigInteger.Parse("66666667", NumberStyles.HexNumber), IntegerSpec.Int32).Normalized);
            Assert.AreEqual(
                new IntegerConstant(5).ComputeSignedDivisionMagic().ShiftAmount, 1);

            Assert.AreEqual(
                new IntegerConstant(7).ComputeSignedDivisionMagic().Multiplier,
                new IntegerConstant(BigInteger.Parse("92492493", NumberStyles.HexNumber), IntegerSpec.Int32).Normalized);
            Assert.AreEqual(
                new IntegerConstant(7).ComputeSignedDivisionMagic().ShiftAmount, 2);
        }

        private IEnumerable<Tuple<int, int>> GetGeneralOpTestValues(Random Rand)
        {
            foreach (var i in GetRandomValues(Rand, 200))
            {
                foreach (var j in GetRandomValues(Rand, 200))
                {
                    yield return Tuple.Create(i, j);
                }
            }
        }

        private IEnumerable<int> GetRandomValues(Random Rand, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                yield return Rand.Next();
            }
        }

        private IEnumerable<int> GetTestValues()
        {
            for (int i = -10000; i < 10000; i++)
            {
                yield return i;
            }

            int pow10 = 100000;
            for (int i = 1; i < 20; i++)
            {
                yield return -pow10;
                yield return pow10;
                pow10 *= 10;
            }

            yield return int.MaxValue;
            yield return int.MinValue;
        }

        private ulong ToUInt(ulong Value, int Size)
        {
            return Value & ((1UL << Size) - 1);
        }

        private void TestOp(
            Func<IntegerConstant, IntegerConstant, IntegerConstant> IntValOp, 
            Func<int, int, int> IntOp,
            IEnumerable<Tuple<int, int>> TestVals)
        {
            foreach (var pair in TestVals)
            {
                Assert.AreEqual(
                    IntValOp(new IntegerConstant(pair.Item1), new IntegerConstant(pair.Item2)).ToInt32(), 
                    IntOp(pair.Item1, pair.Item2));
            }
        }
    }
}


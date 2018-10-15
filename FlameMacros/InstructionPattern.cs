using System.Collections.Generic;
using Flame.Collections;
using Loyc;
using Loyc.Syntax;

namespace FlameMacros
{
    public struct InstructionPattern
    {
        public InstructionPattern(
            Symbol instructionName,
            string prototypeKind,
            IReadOnlyList<LNode> prototypeArgs,
            IReadOnlyList<Symbol> instructionArgs)
        {
            this.InstructionName = instructionName;
            this.PrototypeKind = prototypeKind;
            this.PrototypeArgs = prototypeArgs;
            this.InstructionArgs = instructionArgs;
        }

        public Symbol InstructionName { get; private set; }

        public string PrototypeKind { get; private set; }

        public IReadOnlyList<LNode> PrototypeArgs { get; private set; }

        public IReadOnlyList<Symbol> InstructionArgs { get; private set; }
    }

    /// <summary>
    /// A comparer for instruction patterns that takes only the shape
    /// of prototypes into account.
    /// </summary>
    public sealed class InstructionPatternPrototypeComparer : IEqualityComparer<InstructionPattern>
    {
        private InstructionPatternPrototypeComparer()
        { }

        public static readonly InstructionPatternPrototypeComparer Instance =
            new InstructionPatternPrototypeComparer();

        public bool Equals(InstructionPattern x, InstructionPattern y)
        {
            // We want this function to declare things like this as equal:
            //
            //     intrinsic("arith.convert", To, #(Intermediate))(X);
            //     ==
            //     intrinsic("arith.convert", To, #(From))(Y);
            //
            // That is, the instruction arguments don't matter and neither do
            // the names of the variables.
            //
            // However, we do want to take variable equality into account: the
            // first example below indicates that `To` may be equal to `Intermediate`
            // but the second indicates that the first occurence of `To` must
            // be equal to the second occurence.
            //
            //     intrinsic("arith.convert", To, #(Intermediate))(X);
            //     !=
            //     intrinsic("arith.convert", To, #(To))(Y);

            int count = x.PrototypeArgs.Count;
            if (!x.PrototypeKind.Equals(y.PrototypeKind)
                || count != y.PrototypeArgs.Count)
            {
                return false;
            }

            var leftNumbering = new Dictionary<Symbol, int>();
            var rightNumbering = new Dictionary<Symbol, int>();
            for (int i = 0; i < count; i++)
            {
                var lArg = x.PrototypeArgs[i];
                var rArg = y.PrototypeArgs[i];

                if (!AreEquivalentArgs(lArg, rArg, leftNumbering, rightNumbering))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreEquivalentArgs(
            LNode left,
            LNode right,
            Dictionary<Symbol, int> leftNumbering,
            Dictionary<Symbol, int> rightNumbering)
        {
            if (left.IsLiteral)
            {
                return right.IsLiteral && left.Value.Equals(right.Value);
            }
            else if (left.IsCall)
            {
                int count = left.ArgCount;
                if (!left.IsCall
                    || count != right.Count
                    || !left.Target.Equals(right.Target))
                {
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (!AreEquivalentArgs(left.Args[i], right.Args[i], leftNumbering, rightNumbering))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                // `left` must be an identifier because we know
                // it's neither a literal nor a call.
                if (!right.IsId)
                {
                    return false;
                }

                var leftSymbol = left.Name;
                var rightSymbol = right.Name;

                int leftNumber;
                if (leftNumbering.TryGetValue(leftSymbol, out leftNumber))
                {
                    // If this is not the first time we're encountering
                    // this symbol on the left-hand side, then we the right-hand
                    // side is equivalent if the left-hand side and the
                    // right-hand side have assigned the same number to the
                    // symbol.
                    int rightNumber;
                    return rightNumbering.TryGetValue(rightSymbol, out rightNumber)
                        && leftNumber == rightNumber;
                }
                else
                {
                    // If this is the first time we're encountering this
                    // symbol on the right-hand side, then the right-hand
                    // side is equivalent if it, too, has not encountered
                    // the symbol yet.
                    leftNumbering[leftSymbol] = leftNumbering.Count;
                    if (rightNumbering.ContainsKey(rightSymbol))
                    {
                        return false;
                    }
                    else
                    {
                        rightNumbering[rightSymbol] = rightNumbering.Count;
                        return true;
                    }
                }
            }
        }

        public int GetHashCode(InstructionPattern obj)
        {
            int hashCode = EnumerableComparer.EmptyHash;
            hashCode = EnumerableComparer.FoldIntoHashCode(
                hashCode,
                obj.PrototypeKind.GetHashCode());

            var numbering = new Dictionary<Symbol, int>();
            foreach (var arg in obj.PrototypeArgs)
            {
                hashCode = EnumerableComparer.FoldIntoHashCode(
                    hashCode,
                    HashArg(arg, numbering));
            }

            return hashCode;
        }

        private static int HashArg(LNode argument, Dictionary<Symbol, int> numbering)
        {
            if (argument.IsLiteral)
            {
                return argument.Value.GetHashCode();
            }
            else if (argument.IsCall)
            {
                int hashCode = EnumerableComparer.EmptyHash;
                hashCode = EnumerableComparer.FoldIntoHashCode(
                    hashCode,
                    argument.Target.GetHashCode());

                foreach (var arg in argument.Args)
                {
                    hashCode = EnumerableComparer.FoldIntoHashCode(
                        hashCode,
                        HashArg(arg, numbering));
                }
                return hashCode;
            }
            else
            {
                int number;
                if (!numbering.TryGetValue(argument.Name, out number))
                {
                    number = numbering.Count;
                    numbering[argument.Name] = number;
                }
                return number;
            }
        }
    }
}

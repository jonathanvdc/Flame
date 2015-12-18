using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class PartialOperatorBlock : IPartialBlock
    {
        public PartialOperatorBlock(ICodeGenerator CodeGenerator, IMethod OperatorOverload, ICppBlock OperatorTarget)
        {
            this.CodeGenerator = CodeGenerator;
            this.OperatorOverload = OperatorOverload;
            this.OperatorTarget = OperatorTarget;
        }

        /// <summary>
        /// Gets the operator overload this block encloses.
        /// </summary>
        public IMethod OperatorOverload { get; private set; }

        /// <summary>
        /// Gets an optional first operand that is provided
        /// for this operator overload.
        /// </summary>
        public ICppBlock OperatorTarget { get; private set; }

        /// <summary>
        /// Gets this operator block's operator.
        /// </summary>
        public Operator Operator
        {
            get
            {
                return OperatorOverload.GetOperator();
            }
        }

        /// <summary>
        /// Gets the initial operand sequence this block encloses.
        /// </summary>
        public IEnumerable<ICppBlock> InitialOperands
        {
            get
            {
                return OperatorTarget == null 
                    ? Enumerable.Empty<ICppBlock>() 
                    : new ICppBlock[] { OperatorTarget };
            }
        }

        /// <summary>
        /// Gets the number of initial operands in this partial
        /// operator block.
        /// </summary>
        public int InitialOperandCount
        {
            get
            {
                return OperatorTarget == null ? 0 : 1;
            }
        }

        /// <summary>
        /// Gets this partial operator block's code generator.
        /// </summary>
        public ICodeGenerator CodeGenerator { get; private set; }

        /// <summary>
        /// Gets the total number of operands the overload takes.
        /// </summary>
        public int Arity
        {
            get { return GetArity(OperatorOverload); }
        }

        /// <summary>
        /// Checks if this operator overload is a unary operator.
        /// </summary>
        public bool IsUnary
        {
            get
            {
                return Arity == 1;
            }
        }

        /// <summary>
        /// Checks if this operator overload is a binary operator.
        /// </summary>
        public bool IsBinary
        {
            get
            {
                return Arity == 2;
            }
        }

        /// <summary>
        /// Gets the number of operands this method takes in total,
        /// including its target object.
        /// </summary>
        /// <param name="Method"></param>
        /// <returns></returns>
        public static int GetArity(IMethod Method)
        {
            return (Method.IsStatic ? 0 : 1) + Method.Parameters.Count();
        }

        /// <summary>
        /// Checks if the given operator and arity are in the C++ language.
        /// </summary>
        /// <param name="Op"></param>
        /// <param name="Arity"></param>
        /// <returns></returns>
        public static bool IsSupported(Operator Op, int Arity)
        {
            return (Arity == 1 && UnaryOperation.IsSupported(Op))
                || (Arity == 2 && BinaryOperation.IsSupported(Op));
        }

        /// <summary>
        /// Checks if the given operator overload has a supported
        /// arity and operator.
        /// </summary>
        /// <param name="Method"></param>
        /// <returns></returns>
        public static bool IsSupportedOverload(IMethod Method)
        {
            return IsSupported(Method.GetOperator(), GetArity(Method));
        }

        public ICppBlock Complete(PartialArguments Arguments)
        {
            var totalArgs = InitialOperands.Concat(Arguments.GetArguments(Arity - InitialOperandCount)).ToArray();
            if (IsUnary)
            {
                return new UnaryOperation(CodeGenerator, totalArgs[0], Operator, OperatorOverload);
            }
            else if (IsBinary)
            {
                return new BinaryOperation(CodeGenerator, totalArgs[0], Operator, totalArgs[1], OperatorOverload);
            }
            else
            {
                throw new InvalidOperationException(
                    "C++ does not have overloaded operators with an arity greater than two.");
            }
        }

        public IType Type
        {
            get { return MethodType.Create(OperatorOverload); }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return OperatorOverload.GetDependencies().MergeDependencies(InitialOperands.GetDependencies()); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return InitialOperands.GetUsedLocals(); }
        }

        public CodeBuilder GetCode()
        {
            throw new NotImplementedException();
        }
    }
}

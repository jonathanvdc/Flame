using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Flame.Compiler.Visitors;
using Flame.Compiler;
using Flame.Compiler.Native;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;

namespace Flame.Wasm.Passes
{
    /// <summary>
    /// A pass that lowers aggregate copies to scalar operations.
    /// </summary>
    public class CopyLoweringVisitor : NodeVisitorBase
    {
        public CopyLoweringVisitor(IAbi Abi)
        {
            this.Abi = Abi;
            this.tempPtrs = new Queue<RegisterVariable>();
        }

        /// <summary>
        /// Gets the ABI this copy lowering visitor uses.
        /// </summary>
        public IAbi Abi { get; private set; }

        private Queue<RegisterVariable> tempPtrs;

        private RegisterVariable GetTemporaryPointer()
        {
            if (tempPtrs.Count == 0)
                return new RegisterVariable(Abi.PointerIntegerType);
            else
                return tempPtrs.Dequeue();
        }

        private void ReleaseTemporaryPointer(RegisterVariable Temp)
        {
            tempPtrs.Enqueue(Temp);
        }

        public override bool Matches(IExpression Value)
        {
            return false;
        }

        public override bool Matches(IStatement Value)
        {
            return Value is ISetVariableNode;
        }

        protected override IExpression Transform(IExpression Expression)
        {
            return Expression;
        }

        private static IExpression GetSourcePointer(IExpression Expression)
        {
            if (Expression is IVariableNode)
            {
                var varNode = (IVariableNode)Expression;
                var srcVar = varNode.GetVariable() as IUnmanagedVariable;
                if (varNode.Action == VariableNodeAction.Get && srcVar != null)
                {
                    return srcVar.CreateAddressOfExpression();
                }
            }
            else if (Expression is IMetadataNode<IExpression>)
            {
                var metaNode = (IMetadataNode<IExpression>)Expression;
                return GetSourcePointer(metaNode.Value);
            }
            else if (Expression is InitializedExpression)
            {
                var initExpr = (InitializedExpression)Expression;
                return new InitializedExpression(
                    initExpr.Initialization, 
                    GetSourcePointer(initExpr.Value), 
                    initExpr.Finalization);
            }
            throw new Exception(
                "Could not find a source pointer " +
                "for an aggregate copy operation.");
        }

        protected override IStatement Transform(IStatement Statement)
        {
            var setVarNode = (ISetVariableNode)Statement;
            var targetVar = setVarNode.GetVariable();

            var targetTy = targetVar.Type;
            if (targetTy.IsScalar())
                return Statement.Accept(this);

            var stmts = new List<IStatement>();
            
            var destTemp = GetTemporaryPointer();
            stmts.Add(destTemp.CreateSetStatement(
                new ReinterpretCastExpression(
                    Visit(((IUnmanagedVariable)targetVar).CreateAddressOfExpression()), 
                    Abi.PointerIntegerType).Simplify()));

            var srcTemp = GetTemporaryPointer();
            stmts.Add(srcTemp.CreateSetStatement(
                new ReinterpretCastExpression(
                    GetSourcePointer(Visit(setVarNode.Value)), 
                    Abi.PointerIntegerType).Simplify()));
            
            stmts.Add(CopyLoweringPass.CreateBitwiseCopy(
                srcTemp.CreateGetExpression(), destTemp.CreateGetExpression(), 
                Abi.GetLayout(targetTy), Abi));

            ReleaseTemporaryPointer(destTemp);
            ReleaseTemporaryPointer(srcTemp);

            return new BlockStatement(stmts).Simplify();
        }
    }

    /// <summary>
    /// A pass that lowers aggregate copies to scalar operations.
    /// </summary>
    public class CopyLoweringPass : IPass<IStatement, IStatement>
    {
        public CopyLoweringPass(IAbi Abi)
        {
            this.Abi = Abi;
        }

        /// <summary>
        /// Gets the ABI this copy lowering pass uses.
        /// </summary>
        public IAbi Abi { get; private set; }

        /// <summary>
        /// The name of the copy lowering pass.
        /// </summary>
        public const string CopyLoweringPassName = "lower-copy";

        public IStatement Apply(IStatement Value)
        {
            return new CopyLoweringVisitor(Abi).Visit(Value);
        }

        /// <summary>
        /// 'Dereferences' the given type: a single layer of 
        /// type-level pointer indirection is performed, but
        /// only if that can actually be done.
        /// </summary>
        public static IType DereferenceType(IType Type)
        {
            if (Type.GetIsPointer())
                return Type.AsPointerType().ElementType;
            else
                return Type;
        }

        /// <summary>
        /// Indexes the given base pointer at the given offset, and reinterprets the result
        /// as a pointer of the given type.
        /// </summary>
        public static IExpression IndexPointer(IExpression BasePointer, int Offset, IAbi Abi, IType Type)
        {
            if (Offset == 0)
            {
                // Simple, yet surprisingly common, case
                return new ReinterpretCastExpression(
                    BasePointer, 
                    Type.MakePointerType(PointerKind.ReferencePointer)).Simplify();
            }

            return new ReinterpretCastExpression(
                new AddExpression(
                    new ReinterpretCastExpression(BasePointer, Abi.PointerIntegerType).Simplify(),
                    new StaticCastExpression(new IntegerExpression(Offset), Abi.PointerIntegerType).Simplify()),
                Type.MakePointerType(PointerKind.ReferencePointer));
        }

        /// <summary>
        /// Creates a statement that performs a bitwise copy
        /// of the data in the source memory location to the
        /// destination memory location.
        /// </summary>
        public static IStatement CreateBitwiseCopy(
            IExpression SourcePointer, 
            IExpression DestinationPointer,
            DataLayout Layout, IAbi Abi)
        {
            var instrs = new List<IStatement>();

            int wordSize = Abi.GetLayout(Abi.PointerIntegerType).Size;

            int wordCount = Layout.Size / wordSize;
            int totalWordSize = wordCount * wordSize;
            for (int i = 0; i < wordCount; i++)
            {
                instrs.Add(
                    new StoreAtAddressStatement(
                        IndexPointer(DestinationPointer, i * wordSize, Abi, Abi.PointerIntegerType),
                        new DereferencePointerExpression(
                            IndexPointer(SourcePointer, i * wordSize, Abi, Abi.PointerIntegerType))));
            }
            for (int i = totalWordSize; i < Layout.Size; i++)
            {
                instrs.Add(
                    new StoreAtAddressStatement(
                        IndexPointer(DestinationPointer, i, Abi, PrimitiveTypes.Bit8),
                        new DereferencePointerExpression(
                            IndexPointer(SourcePointer, i, Abi, PrimitiveTypes.Bit8))));
            }
            return new BlockStatement(instrs).Simplify();
        }
    }
}


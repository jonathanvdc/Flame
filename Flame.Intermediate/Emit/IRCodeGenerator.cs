using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler.Variables;
using Flame.Compiler.Flow;

namespace Flame.Intermediate.Emit
{
    public class IRCodeGenerator : ICodeGenerator, IExceptionCodeGenerator,
                                   IUnmanagedCodeGenerator, IWhileCodeGenerator,
                                   IDoWhileCodeGenerator, IForCodeGenerator,
                                   IForeachCodeGenerator, ICommentedCodeGenerator,
                                   IYieldCodeGenerator, ILambdaCodeGenerator,
                                   IInitializingCodeGenerator, IContractCodeGenerator,
								   ISSACodeGenerator, IBlockCodeGenerator,
								   IStackCodeGenerator
    {
        public IRCodeGenerator(IRAssemblyBuilder Assembly, IMethod Method)
        {
            this.Assembly = Assembly;
            this.Method = Method;

            this.variableNames = new UniqueNameSet<IVariableMember>(item => item.Name, "%");
            this.tags = new UniqueNameMap<UniqueTag>(item => item.Name, "!");
            this.postprocessNode = x => x;
        }

        public IRAssemblyBuilder Assembly { get; private set; }
        public IMethod Method { get; private set; }

        private UniqueNameMap<UniqueTag> tags;
        private UniqueNameSet<IVariableMember> variableNames;
        private Func<LNode, LNode> postprocessNode;

        #region Postprocessing

        /// <summary>
        /// "Postprocesses" the given node: nodes
		/// may be inserted by this pass
        /// for a number of purposes, such as variable declaration.
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public ICodeBlock Postprocess(ICodeBlock Block)
        {
            return new NodeBlock(this, postprocessNode(NodeBlock.ToNode(Block)));
        }

        #endregion

        #region Literals

        public NodeBlock EmitLiteral(object Value, string LiteralName)
        {
            return new NodeBlock(this, NodeFactory.Call(LiteralName, new LNode[] { NodeFactory.Literal(Value) }));
        }

        #region Bit types

        public ICodeBlock EmitBit8(byte Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantBit8Name);
        }

        public ICodeBlock EmitBit16(ushort Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantBit16Name);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantBit32Name);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantBit64Name);
        }

        #endregion

        #region Signed integer types

        public ICodeBlock EmitInt16(short Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantInt16Name);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantInt32Name);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantInt64Name);
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantInt8Name);
        }

        #endregion

        #region Unsigned integer types

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantUInt16Name);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantUInt32Name);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantUInt64Name);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantUInt8Name);
        }

        #endregion

        #region Floating point types

        public ICodeBlock EmitFloat32(float Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantFloat32Name);
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantFloat64Name);
        }

        #endregion

        #region Miscellaneous types

        public ICodeBlock EmitBoolean(bool Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantBooleanName);
        }

        public ICodeBlock EmitChar(char Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantCharName);
        }

        public ICodeBlock EmitString(string Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantStringName);
        }

        #endregion

        #region Void

        public ICodeBlock EmitVoid()
        {
            return new NodeBlock(this, NodeFactory.Id(ExpressionParsers.ConstantVoidName));
        }

        #endregion

        #region Null

        public ICodeBlock EmitNull()
        {
            return new NodeBlock(this, NodeFactory.Id(ExpressionParsers.ConstantNullName));
        }

        #endregion

        #region Default

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.ConstantDefaultName, new[]
            {
                Assembly.TypeTable.GetReference(Type)
            }));
        }

        #endregion

        #endregion

        #region Operators

        private static readonly Dictionary<Operator, string> typeBinaryOpNames = new Dictionary<Operator, string>()
        {
            { Operator.DynamicCast, ExpressionParsers.DynamicCastNode },
            { Operator.StaticCast, ExpressionParsers.StaticCastNode },
            { Operator.ReinterpretCast, ExpressionParsers.ReinterpretCastNode },
            { Operator.IsInstance, ExpressionParsers.IsInstanceNode },
            { Operator.AsInstance, ExpressionParsers.AsInstanceNode }
        };

        public ICodeBlock EmitPop(ICodeBlock Value)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.IgnoreNodeName, new[]
            {
                NodeBlock.ToNode(Value)
            }));
        }

        public ICodeBlock EmitTypeBinary(ICodeBlock Value, IType Type, Operator Op)
        {
            string opName;
            if (typeBinaryOpNames.TryGetValue(Op, out opName))
            {
                return new NodeBlock(this, NodeFactory.Call(opName, new[]
                {
                    Assembly.TypeTable.GetReference(Type),
                    NodeBlock.ToNode(Value)
                }));
            }
            else
            {
                return null; // Null signals that this can't be done.
            }
        }

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.BinaryNode, new[]
            {
                NodeBlock.ToNode(A),
                NodeFactory.Literal(Op.Name),
                NodeBlock.ToNode(B)
            }));
        }

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.UnaryNode, new[]
            {
                NodeFactory.Literal(Op.Name),
                NodeBlock.ToNode(Value)
            }));
        }

        #endregion

        #region Interprocedural control flow

        public ICodeBlock EmitReturn(ICodeBlock Value)
        {
            return NodeBlock.Call(this, ExpressionParsers.ReturnNodeName, Value == null ? new LNode[0] : new[]
            {
                NodeBlock.ToNode(Value)
            });
        }

        #region Yield

        public ICodeBlock EmitYieldBreak()
        {
            return NodeBlock.Id(this, ExpressionParsers.YieldBreakNodeName);
        }

        public ICodeBlock EmitYieldReturn(ICodeBlock Value)
        {
            return NodeBlock.Call(this, ExpressionParsers.YieldReturnNodeName, Value);
        }

        #endregion

        #endregion

        #region Intraprocedural control flow

        public ICodeBlock EmitIfElse(ICodeBlock Condition, ICodeBlock IfBody, ICodeBlock ElseBody)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.SelectNodeName, new[]
            {
                NodeBlock.ToNode(Condition),
                NodeBlock.ToNode(IfBody),
                NodeBlock.ToNode(ElseBody)
            }));
        }

        public ICodeBlock EmitSequence(ICodeBlock First, ICodeBlock Second)
        {
            return new NodeBlock(this, NodeFactory.MergedBlock(NodeBlock.ToNode(First), NodeBlock.ToNode(Second)));
        }

        public ICodeBlock EmitTagged(UniqueTag Tag, ICodeBlock Contents)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.TaggedNodeName, new[]
            {
                EmitTagNode(Tag),
                NodeBlock.ToNode(Contents)
            }));
        }

        public ICodeBlock EmitBreak(UniqueTag Target)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.BreakNodeName, new[]
            {
                NodeFactory.Call(ExpressionParsers.TagReferenceName, new[] { EmitTagNode(Target) })
            }));
        }

        public ICodeBlock EmitContinue(UniqueTag Target)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.ContinueNodeName, new[]
            {
                NodeFactory.Call(ExpressionParsers.TagReferenceName, new[] { EmitTagNode(Target) })
            }));
        }

        public ICodeBlock EmitWhile(UniqueTag Tag, ICodeBlock Condition, ICodeBlock Body)
        {
            return NodeBlock.Call(this, ExpressionParsers.WhileNodeName, EmitTagNode(Tag), NodeBlock.ToNode(Condition), NodeBlock.ToNode(Body));
        }

        public ICodeBlock EmitDoWhile(UniqueTag Tag, ICodeBlock Body, ICodeBlock Condition)
        {
            return NodeBlock.Call(this, ExpressionParsers.DoWhileNodeName, EmitTagNode(Tag), NodeBlock.ToNode(Body), NodeBlock.ToNode(Condition));
        }

        public ICodeBlock EmitForBlock(UniqueTag Tag, ICodeBlock Initialization, ICodeBlock Condition, ICodeBlock Delta, ICodeBlock Body)
        {
            return NodeBlock.Call(this, ExpressionParsers.ForNodeName,
                EmitTagNode(Tag), NodeBlock.ToNode(Initialization),
                NodeBlock.ToNode(Condition), NodeBlock.ToNode(Delta),
                NodeBlock.ToNode(Body), NodeBlock.ToNode(EmitVoid()));
        }

        #region Foreach

        public ICollectionBlock EmitCollectionBlock(IVariableMember Member, ICodeBlock Collection)
        {
            return new NodeCollectionBlock(this, Member, NodeBlock.ToNode(Collection));
        }

        public ICodeBlock EmitForeachBlock(IForeachBlockHeader Header, ICodeBlock Body)
        {
            var header = (NodeForeachHeader)Header;

            return NodeBlock.Call(this, ExpressionParsers.ForeachNodeName,
                header.TagNode, header.CollectionsNode, NodeBlock.ToNode(Body));
        }

        public IForeachBlockHeader EmitForeachHeader(UniqueTag Tag, IEnumerable<ICollectionBlock> Collections)
        {
            var tagNode = EmitTagNode(Tag);
            var elems = Collections.Cast<NodeCollectionBlock>().Select(item =>
            {
                string name = variableNames.GenerateName(item.Member);
                var localVar = new NodeEmitVariable(this, ExpressionParsers.LocalVariableKindName, NodeFactory.IdOrLiteral(name));
                return Tuple.Create(name, localVar, item);
            }).ToArray();

            return new NodeForeachHeader(tagNode, elems);
        }

        #endregion

        #endregion

        #region Delegates

        private static readonly Dictionary<Operator, string> delegateOperatorNames = new Dictionary<Operator, string>()
        {
            { Operator.GetDelegate, ExpressionParsers.GetDelegateNodeName },
            { Operator.GetVirtualDelegate, ExpressionParsers.GetVirtualDelegateNodeName },
            { Operator.GetCurriedDelegate, ExpressionParsers.GetCurriedDelegateNodeName }
        };

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller, Operator Op)
        {
            string opName;
            if (delegateOperatorNames.TryGetValue(Op, out opName))
            {
                return new NodeBlock(this, NodeFactory.Call(opName, new[]
                {
                    Assembly.MethodTable.GetReference(Method),
                    NodeBlock.ToNode(Caller)
                }));
            }
            else
            {
                return null; // Null signals that this can't be done.
            }
        }

        #endregion

        #region Invocations

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.InvocationNodeName, new[]
            {
                NodeBlock.ToNode(Method)
            }.Concat(Arguments.Select(NodeBlock.ToNode))));
        }

        #endregion

        #region Container types

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.NewArrayName, new[]
            {
                Assembly.TypeTable.GetReference(ElementType)
            }.Concat(Dimensions.Select(NodeBlock.ToNode))));
        }

        public ICodeBlock EmitNewVector(IType ElementType, IReadOnlyList<int> Dimensions)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.NewVectorName, new[]
            {
                Assembly.TypeTable.GetReference(ElementType)
            }.Concat(Dimensions.Select(item => NodeFactory.VarLiteral(item)))));
        }

        public ICodeBlock EmitInitializedArray(IType ElementType, ICodeBlock[] Items)
        {
            return NodeBlock.Call(this, ExpressionParsers.NewInitializedArrayName,
                new LNode[] { Assembly.TypeTable.GetReference(ElementType) }
                .Concat(Items.Select(NodeBlock.ToNode)).ToArray());
        }

        public ICodeBlock EmitInitializedVector(IType ElementType, ICodeBlock[] Items)
        {
            return NodeBlock.Call(this, ExpressionParsers.NewInitializedVectorName,
                new LNode[] { Assembly.TypeTable.GetReference(ElementType) }
                .Concat(Items.Select(NodeBlock.ToNode)).ToArray());
        }

        #endregion

        #region Variables

        private Dictionary<UniqueTag, NodeEmitVariable> locals = new Dictionary<UniqueTag, NodeEmitVariable>();

        public IUnmanagedEmitVariable GetUnmanagedLocal(UniqueTag Tag)
        {
            NodeEmitVariable result;
            if (locals.TryGetValue(Tag, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public IUnmanagedEmitVariable DeclareUnmanagedLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            string name = variableNames.GenerateName(VariableMember);
            var sig = EmitSignature(VariableMember);
            var type = Assembly.TypeTable.GetReference(VariableMember.VariableType);

            var oldPostprocessor = this.postprocessNode;
            this.postprocessNode = body => oldPostprocessor(NodeFactory.Call(ExpressionParsers.DefineLocalNodeName, new LNode[]
            {
                NodeFactory.IdOrLiteral(name),
                sig.Node,
                type,
                body
            }));
            var result = new NodeEmitVariable(this, ExpressionParsers.LocalVariableKindName, NodeFactory.IdOrLiteral(name));
            locals.Add(Tag, result);
            return result;
        }

        public IEmitVariable GetLocal(UniqueTag Tag)
        {
            return GetUnmanagedLocal(Tag);
        }

        public IEmitVariable DeclareLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            return DeclareUnmanagedLocal(Tag, VariableMember);
        }

		public IEmitVariable GetSSALocal(UniqueTag Tag)
		{
			return GetUnmanagedLocal(Tag);
		}

		public IEmitVariable DeclareSSALocal(UniqueTag Tag, IVariableMember VariableMember)
		{
			string name = variableNames.GenerateName(VariableMember);
			var sig = EmitSignature(VariableMember);
			var type = Assembly.TypeTable.GetReference(VariableMember.VariableType);

			var oldPostprocessor = this.postprocessNode;
			this.postprocessNode = body => oldPostprocessor(NodeFactory.Call(ExpressionParsers.DefineSSALocalNodeName, new LNode[]
			{
				NodeFactory.IdOrLiteral(name),
				sig.Node,
				type,
				body
			}));
			var result = new NodeEmitVariable(this, ExpressionParsers.LocalVariableKindName, NodeFactory.IdOrLiteral(name));
			locals.Add(Tag, result);
			return result;
		}

        public IUnmanagedEmitVariable GetUnmanagedArgument(int Index)
        {
            return new NodeEmitVariable(this, ExpressionParsers.ArgumentVariableKindName, NodeFactory.VarLiteral(Index));
        }

        public IEmitVariable GetArgument(int Index)
        {
            return GetUnmanagedArgument(Index);
        }

        public IUnmanagedEmitVariable GetUnmanagedThis()
        {
            return new NodeEmitVariable(this, ExpressionParsers.ThisVariableKindName);
        }

        public IEmitVariable GetThis()
        {
            return GetUnmanagedThis();
        }

        public IUnmanagedEmitVariable GetUnmanagedElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return new NodeEmitVariable(this, ExpressionParsers.ElementVariableKindName,
                new[] { NodeBlock.ToNode(Value) }
                .Concat(Index.Select(NodeBlock.ToNode)));
        }

        public IEmitVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return GetUnmanagedElement(Value, Index);
        }

        public IUnmanagedEmitVariable GetUnmanagedField(IField Field, ICodeBlock Target)
        {
            return new NodeEmitVariable(this, ExpressionParsers.FieldVariableKindName,
                Assembly.FieldTable.GetReference(Field),
                NodeBlock.ToNode(Target));
        }

        public IEmitVariable GetField(IField Field, ICodeBlock Target)
        {
            return GetUnmanagedField(Field, Target);
        }

        #endregion

        #region Exceptions

        public ICodeBlock EmitAssert(ICodeBlock Condition)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.AssertNodeName, new LNode[] { NodeBlock.ToNode(Condition) }));
        }

        public ICodeBlock EmitThrow(ICodeBlock Exception)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.ThrowNodeName, new LNode[] { NodeBlock.ToNode(Exception) }));
        }

        public ICatchClause EmitCatchClause(ICatchHeader Header, ICodeBlock Body)
        {
            return new NodeCatchClause((NodeCatchHeader)Header, NodeBlock.ToNode(Body));
        }

        public ICatchHeader EmitCatchHeader(IVariableMember ExceptionVariable)
        {
            return new NodeCatchHeader(this, variableNames.GenerateName(ExceptionVariable), ExceptionVariable);
        }

        public ICodeBlock EmitTryBlock(ICodeBlock TryBody, ICodeBlock FinallyBody, IEnumerable<ICatchClause> CatchClauses)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.TryNodeName, new LNode[]
            {
                NodeBlock.ToNode(TryBody),
                NodeFactory.Block(CatchClauses.Cast<NodeCatchClause>().Select(item => item.Node)),
                NodeBlock.ToNode(FinallyBody)
            }));
        }

        #endregion

        #region Lambdas

        public ICodeBlock EmitLambda(ILambdaHeaderBlock Header, ICodeBlock Body)
        {
            return new NodeBlock(this, ((NodeLambdaHeader)Header).CreateLambdaNode(NodeBlock.ToNode(Body)));
        }

        public ILambdaHeaderBlock EmitLambdaHeader(IMethod Member, IEnumerable<ICodeBlock> CapturedValues)
        {
            return new NodeLambdaHeader(this, Member, CapturedValues.Select(NodeBlock.ToNode).ToArray());
        }

        #endregion

        #region Unmanaged

        public ICodeBlock EmitDereferencePointer(ICodeBlock Pointer)
        {
            return NodeBlock.Call(this, ExpressionParsers.DereferenceName, Pointer);
        }

        public ICodeBlock EmitSizeOf(IType Type)
        {
            return NodeBlock.Call(this, ExpressionParsers.SizeOfName, Assembly.TypeTable.GetReference(Type));
        }

        public ICodeBlock EmitStoreAtAddress(ICodeBlock Pointer, ICodeBlock Value)
        {
            return NodeBlock.Call(this, ExpressionParsers.StoreAtName, Pointer, Value);
        }

        #endregion

        #region Comments/Debugging

        public ICodeBlock EmitComment(string Comment)
        {
            return NodeBlock.Call(this, ExpressionParsers.CommentNodeName, NodeFactory.Literal(Comment));
        }

        #endregion

        #region Helpers

        public LNode EmitTagNode(UniqueTag Tag)
        {
            return NodeFactory.IdOrLiteral(tags[Tag]);
        }

        /// <summary>
        /// Creates an IR signature from the given variable member.
        /// </summary>
        /// <param name="Member"></param>
        /// <returns></returns>
        public IRSignature EmitSignature(IVariableMember Member)
        {
            return IREmitHelpers.CreateSignature(Assembly, Member.Name, Member.Attributes);
        }

        #endregion

		#region IContractCodeGenerator implementation

		/// <summary>
		/// Gets the contract method's return value variable.
		/// </summary>
		/// <value>The return variable.</value>
		public IEmitVariable ReturnVariable
		{
			get
			{
				return new NodeEmitVariable(this, ExpressionParsers.ReturnValueVariableKindName, Enumerable.Empty<LNode>());
			}
		}

		/// <summary>
		/// Emits a contract block.
		/// </summary>
		/// <returns>The contract block.</returns>
		/// <param name="Preconditions">Preconditions.</param>
		/// <param name="Postconditions">Postconditions.</param>
		/// <param name="Body">Body.</param>
		public ICodeBlock EmitContractBlock(
			IEnumerable<ICodeBlock> Preconditions, IEnumerable<ICodeBlock> Postconditions,
			ICodeBlock Body)
		{
			return NodeBlock.Call(this, ExpressionParsers.ContractNodeName,
				Body,
				NodeBlock.Block(this, Preconditions),
				NodeBlock.Block(this, Postconditions));
		}

		#endregion

		#region IBlockCodeGenerator implementation

		public LNode EmitSSALocalTagNode(SSAVariable Local)
		{
			return ((NodeEmitVariable)Local.GetEmitVariable(this)).VariableArguments.Single();
		}

		public NodeBlock EmitBasicBlockBranch(BlockBranch Branch)
		{
			// Create a "#branch(target_tag, args...)" node

			return NodeBlock.Call(this, ExpressionParsers.BranchNodeName,
				new LNode[] { EmitTagNode(Branch.TargetTag) }.Concat(
					Branch.Arguments.Select(EmitSSALocalTagNode)).ToArray());
		}

		public NodeBlock EmitBasicBlockFlow(BlockFlow Flow)
		{
            if (Flow is UnreachableFlow)
            {
                // Create an "#unreachable" node
                return NodeBlock.Id(this, ExpressionParsers.UnreachableFlowNodeName);
            }
            else if (Flow is TerminatedFlow)
            {
                // Create a "#terminated" node
                return NodeBlock.Id(this, ExpressionParsers.TerminatedFlowNodeName);
            }
            else if (Flow is ExitFlow)
            {
                // Create an "#exit" node
                return NodeBlock.Id(this, ExpressionParsers.ExitFlowNodeName);
            }
            else if (Flow is JumpFlow)
            {
                // Create a "#jump(#branch(...))" node
                return NodeBlock.Call(this, ExpressionParsers.JumpFlowNodeName,
                    EmitBasicBlockBranch(((JumpFlow)Flow).Branch));
            }
            else if (Flow is SelectFlow)
            {
                // Create a "#select(cond, #branch(...), #branch(...))" node
                var selectFlow = (SelectFlow)Flow;
                return NodeBlock.Call(this, ExpressionParsers.SelectFlowNodeName,
                    selectFlow.Condition.Emit(this),
                    EmitBasicBlockBranch(selectFlow.ThenBranch),
                    EmitBasicBlockBranch(selectFlow.ElseBranch));
            }
            else if (Flow is TryFlow)
            {
                // Create a "#try(#branch(...))" node
                return NodeBlock.Call(this, ExpressionParsers.TryFlowNodeName,
                    EmitBasicBlockBranch(((TryFlow)Flow).Branch));
            }
            else if (Flow is FinallyFlow)
            {
                // Create a "#finally(#branch(...))" node
                return NodeBlock.Call(this, ExpressionParsers.FinallyFlowNodeName,
                    EmitBasicBlockBranch(((FinallyFlow)Flow).Branch));
            }
            else if (Flow is LeaveFlow)
            {
                // Create a "#leave(#branch(...))" node
                return NodeBlock.Call(this, ExpressionParsers.LeaveFlowNodeName,
                    EmitBasicBlockBranch(((LeaveFlow)Flow).Branch));
            }
            else if (Flow is GuardedFlow)
            {
                // Create an "#guarded(#branch(...) #branch(...), { #catch(type, #branch(...))... })" node
                var ehFlow = (GuardedFlow)Flow;
                var handlerList = new List<LNode>(ehFlow.ExceptionBranches.Count);
                foreach (var br in ehFlow.ExceptionBranches)
                {
                    handlerList.Add(NodeFactory.Call(
                        ExpressionParsers.CatchNodeName,
                        new LNode[]
                        {
                            Assembly.TypeTable.GetReference(br.ExceptionType),
                            EmitBasicBlockBranch(br.Branch).Node
                        }));
                }
                return NodeBlock.Call(
                    this, ExpressionParsers.GuardedFlowNodeName,
                    EmitBasicBlockBranch(ehFlow.GuardedBranch).Node,
                    EmitBasicBlockBranch(ehFlow.FinallyBranch).Node,
                    NodeFactory.Block(handlerList));
            }
			else
			{
				throw new InvalidOperationException("Could not encode '" + Flow.ToString() + "' flow.");
			}
		}

		public IEmitBasicBlock EmitBasicBlock(
			UniqueTag Tag, IReadOnlyList<SSAVariable> Parameters,
			ICodeBlock Contents, BlockFlow Flow)
		{
			// Create a "#basic_block(tag, { parameters... }, body, flow)"
			// node

			return new EmitBasicBlock(Tag, NodeBlock.Call(
				this, ExpressionParsers.BasicBlockNodeName,
				EmitTagNode(Tag), NodeFactory.Block(Parameters.Select(EmitSSALocalTagNode)),
				NodeBlock.ToNode(Contents), EmitBasicBlockFlow(Flow).Node).Node);
		}

		public ICodeBlock EmitFlowGraph(
			UniqueTag EntryPointTag, IEnumerable<IEmitBasicBlock> Blocks)
		{
			// Create a "#flow_graph(entry_point_tag, { blocks... })" node

			return NodeBlock.Call(this, ExpressionParsers.FlowGraphNodeName, new LNode[]
            {
                EmitTagNode(EntryPointTag),
                NodeFactory.Block(Blocks.Select(item => ((EmitBasicBlock)item).Node))
            });
		}

        public ICodeBlock EmitCaughtException(IType Type)
        {
            return NodeBlock.Call(this, ExpressionParsers.CaughtExceptionNodeName, Assembly.TypeTable.GetReference(Type));
        }

		#endregion

		#region Stack intrinsics

		public ICodeBlock EmitPush(ICodeBlock Value)
		{
			return NodeBlock.Call(this, ExpressionParsers.PushStackName, Value);
		}

		public ICodeBlock EmitPeek(IType Type)
		{
			return NodeBlock.Call(this, ExpressionParsers.PeekStackName, Assembly.TypeTable.GetReference(Type));
		}

		public ICodeBlock EmitPop(IType Type)
		{
			return NodeBlock.Call(this, ExpressionParsers.PopStackName, Assembly.TypeTable.GetReference(Type));
		}

		#endregion
    }
}

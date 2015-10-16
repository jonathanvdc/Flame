using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRCodeGenerator : ICodeGenerator
    {
        public IRCodeGenerator(IRAssemblyBuilder Assembly, IMethod Method)
        {
            this.Assembly = Assembly;
            this.Method = Method;

            this.variableNames = new UniqueNameSet<IVariableMember>(item => item.Name, "%");
            this.tags = new UniqueNameMap<BlockTag>(item => item.Name, "tag_");
            this.postprocessNode = x => x;
        }

        public IRAssemblyBuilder Assembly { get; private set; }
        public IMethod Method { get; private set; }

        private UniqueNameMap<BlockTag> tags;
        private UniqueNameSet<IVariableMember> variableNames;
        private Func<LNode, LNode> postprocessNode;

        #region Postprocessing

        /// <summary>
        /// "Postprocesses" the given node: nodes
        /// for a number of purposes, such as variable declaration,
        /// may be inserted by this pass.
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
            return new NodeBlock(this, NodeFactory.Literal(Value));
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
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.ReturnNodeName, Value == null ? new LNode[0] : new[] 
            {
                NodeBlock.ToNode(Value) 
            }));
        }

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

        public ICodeBlock EmitTagged(BlockTag Tag, ICodeBlock Contents)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.TaggedNodeName, new[]
            {
                NodeFactory.IdOrLiteral(tags[Tag]),
                NodeBlock.ToNode(Contents)
            }));
        }

        public ICodeBlock EmitBreak(BlockTag Target)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.BreakNodeName, new[]
            {
                NodeFactory.Call(ExpressionParsers.TagReferenceName, new[] { NodeFactory.IdOrLiteral(tags[Target]) })
            }));
        }

        public ICodeBlock EmitContinue(BlockTag Target)
        {
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.ContinueNodeName, new[]
            {
                NodeFactory.Call(ExpressionParsers.TagReferenceName, new[] { NodeFactory.IdOrLiteral(tags[Target]) })
            }));
        }

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
            return new NodeBlock(this, NodeFactory.Call(ExpressionParsers.NewArrayName, new[] 
            { 
                Assembly.TypeTable.GetReference(ElementType) 
            }.Concat(Dimensions.Select(item => NodeFactory.Literal(item)))));
        }

        #endregion

        #region Variables

        public IEmitVariable DeclareVariable(IVariableMember VariableMember)
        {
            string name = variableNames.GenerateName(VariableMember);
            var sig = IREmitHelpers.CreateSignature(Assembly, VariableMember.Name, VariableMember.Attributes);
            var type = Assembly.TypeTable.GetReference(VariableMember.VariableType);

            var oldPostprocessor = this.postprocessNode;
            this.postprocessNode = body => oldPostprocessor(NodeFactory.Call(ExpressionParsers.DefineLocalNodeName, new LNode[]
            {
                NodeFactory.IdOrLiteral(name),
                sig.Node,
                type,
                body
            }));
            return new NodeEmitVariable(this, ExpressionParsers.LocalVariableKindName, NodeFactory.IdOrLiteral(name));
        }

        public IEmitVariable GetArgument(int Index)
        {
            return new NodeEmitVariable(this, ExpressionParsers.ArgumentVariableKindName, NodeFactory.Literal(Index));
        }

        public IEmitVariable GetThis()
        {
            return new NodeEmitVariable(this, ExpressionParsers.ThisVariableKindName);
        }

        public IEmitVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return new NodeEmitVariable(this, ExpressionParsers.ElementVariableKindName, 
                new[] { NodeBlock.ToNode(Value) }
                .Concat(Index.Select(NodeBlock.ToNode)));
        }

        public IEmitVariable GetField(IField Field, ICodeBlock Target)
        {
            return new NodeEmitVariable(this, ExpressionParsers.FieldVariableKindName,
                Assembly.FieldTable.GetReference(Field),
                NodeBlock.ToNode(Target));
        }

        #endregion
    }
}

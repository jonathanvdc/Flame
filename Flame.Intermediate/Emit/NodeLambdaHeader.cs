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
    public class NodeLambdaHeader : ILambdaHeaderBlock
    {
        public NodeLambdaHeader(IRAssemblyBuilder Assembly, IMethod Member, IReadOnlyList<LNode> CaptureList)
        {
            this.CaptureList = CaptureList;
            this.LambdaCodeGenerator = new IRCodeGenerator(Assembly, Member);
        }
        public NodeLambdaHeader(IRCodeGenerator ParentCodeGenerator, IMethod Member, IReadOnlyList<LNode> CaptureList)
            : this(ParentCodeGenerator.Assembly, Member, CaptureList)
        { }

        
        public IReadOnlyList<LNode> CaptureList { get; private set; }
        public IRCodeGenerator LambdaCodeGenerator { get; private set; }

        public IMethod Member { get { return LambdaCodeGenerator.Method; } }

        public ICodeBlock ThisLambdaBlock
        {
            get
            {
                return NodeBlock.Call(LambdaCodeGenerator, ExpressionParsers.RecursiveLambdaDelegateNodeName, new LNode[0]);
            }
        }

        public ICodeBlock EmitGetCapturedValue(int Index)
        {
            return NodeBlock.Call(LambdaCodeGenerator, ExpressionParsers.CapturedValueNodeName, NodeFactory.VarLiteral(Index));
        }

        ICodeGenerator ILambdaHeaderBlock.LambdaCodeGenerator
        {
            get { return LambdaCodeGenerator; }
        }

        public LNode CreateLambdaNode(LNode Body)
        {
            // Format:
            // 
            // #lambda(#member(name, attrs...), return_type, { parameter... }, { captured_exprs... }, body)

            return NodeFactory.Call(ExpressionParsers.LambdaNodeName, new LNode[]
            {
                IREmitHelpers.CreateSignature(LambdaCodeGenerator.Assembly, Member.Name, Member.Attributes).Node,
                LambdaCodeGenerator.Assembly.TypeTable.GetReference(Member.ReturnType),
                IREmitHelpers.ConvertParameters(LambdaCodeGenerator.Assembly, Member.Parameters).Node,
                NodeFactory.Block(CaptureList),
                Body
            });
        }
    }
}

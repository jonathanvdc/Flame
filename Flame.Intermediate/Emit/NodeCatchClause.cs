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
    public class NodeCatchClause : ICatchClause
    {
        public NodeCatchClause(NodeCatchHeader Header, LNode Body)
        {
            this.Header = Header;
            this.Body = Body;
        }

        public NodeCatchHeader Header { get; private set; }
        public LNode Body { get; private set; }

        public IRCodeGenerator CodeGenerator { get { return Header.CodeGenerator; } }

        ICatchHeader ICatchClause.Header
        {
            get { return Header; }
        }

        public LNode Node
        {
            get
            {
                var member = Header.ExceptionVariableMember;

                var sig = IREmitHelpers.CreateSignature(CodeGenerator.Assembly, member.Name, member.Attributes);
                var type = CodeGenerator.Assembly.TypeTable.GetReference(member.VariableType);

                return NodeFactory.Call(ExpressionParsers.CatchNodeName, new LNode[] 
                { 
                    NodeFactory.IdOrLiteral(Header.ExceptionVariableIdentifier),
                    sig.Node,
                    type,
                    Body
                });
            }
        }
    }

    public class NodeCatchHeader : ICatchHeader
    {
        public NodeCatchHeader(IRCodeGenerator CodeGenerator, string ExceptionVariableIdentifier, IVariableMember ExceptionVariableMember)
        {
            this.CodeGenerator = CodeGenerator;
            this.ExceptionVariableIdentifier = ExceptionVariableIdentifier;
            this.ExceptionVariableMember = ExceptionVariableMember;
        }

        public IRCodeGenerator CodeGenerator { get; private set; }
        public string ExceptionVariableIdentifier { get; private set; }
        public IVariableMember ExceptionVariableMember { get; private set; }
        
        public IEmitVariable ExceptionVariable
        {
            get { return new NodeEmitVariable(CodeGenerator, ExpressionParsers.LocalVariableKindName, NodeFactory.IdOrLiteral(ExceptionVariableIdentifier)); }
        }
    }
}

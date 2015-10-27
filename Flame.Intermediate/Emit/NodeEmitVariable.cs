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
    public class NodeEmitVariable : IUnmanagedEmitVariable
    {
        public NodeEmitVariable(IRCodeGenerator CodeGenerator, string VariableKindName, params LNode[] VariableArguments)
            : this(CodeGenerator, VariableKindName, (IEnumerable<LNode>)VariableArguments)
        { }
        public NodeEmitVariable(IRCodeGenerator CodeGenerator, string VariableKindName, IEnumerable<LNode> VariableArguments)
        {
            this.CodeGenerator = CodeGenerator;
            this.VariableKindName = VariableKindName;
            this.VariableArguments = VariableArguments;
        }

        /// <summary>
        /// Gets the code generator this node emit variable is associated with.
        /// </summary>
        public IRCodeGenerator CodeGenerator { get; private set; }

        /// <summary>
        /// Gets the variable kind's name.
        /// </summary>
        public string VariableKindName { get; private set; }

        /// <summary>
        /// Gets the variable's standard node arguments.
        /// </summary>
        public IEnumerable<LNode> VariableArguments { get; private set; }

        public ICodeBlock EmitGet()
        {
            return new NodeBlock(CodeGenerator, 
                NodeFactory.Call(ExpressionParsers.CreateGetVariableName(VariableKindName), VariableArguments));
        }

        public ICodeBlock EmitRelease()
        {
            return new NodeBlock(CodeGenerator, 
                NodeFactory.Call(ExpressionParsers.CreateReleaseVariableName(VariableKindName), VariableArguments));
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return new NodeBlock(CodeGenerator, 
                NodeFactory.Call(ExpressionParsers.CreateSetVariableName(VariableKindName), 
                    VariableArguments.Concat(new LNode[] { NodeBlock.ToNode(Value) })));
        }

        public ICodeBlock EmitAddressOf()
        {
            return new NodeBlock(CodeGenerator,
                NodeFactory.Call(ExpressionParsers.CreateAddressOfVariableName(VariableKindName), VariableArguments));
        }
    }
}

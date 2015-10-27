using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    /// <summary>
    /// A void-returning expression that wraps a block tag instance.
    /// </summary>
    public class TagReferenceExpression : IExpression
    {
        public TagReferenceExpression(BlockTag Tag)
        {
            this.Tag = Tag;
        }

        /// <summary>
        /// Gets the tag wrapped by this tag-reference expression.
        /// </summary>
        public BlockTag Tag { get; private set; }

        public IExpression Accept(INodeVisitor Visitor)
        {
            return this;
        }

        public IBoundObject Evaluate()
        {
            return null;
        }

        public bool IsConstant
        {
            get { return true; }
        }

        public IExpression Optimize()
        {
            return this;
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public ICodeBlock Emit(ICodeGenerator CodeGenerator)
        {
            return CodeGenerator.EmitVoid();
        }
    }
}

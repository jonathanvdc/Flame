using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public abstract class AnalyzedVariableBase : IUnmanagedVariable, IAnalyzedVariable
    {
        public AnalyzedVariableBase(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.InliningCache = new VariableInliningCache(this);
        }

        public abstract IVariable GetVariable(VariableMetrics Metrics);

        /// <summary>
        /// Gets the analyzed variable's type.
        /// </summary>
        public abstract IType Type { get; }

        /// <summary>
        /// Gets a boolean value that tells if the variable is local.
        /// </summary>
        public abstract bool IsLocal { get; }

        /// <summary>
        /// Gets the analyzed variable's code generator.
        /// </summary>
        public ICodeGenerator CodeGenerator { get; private set; }

        /// <summary>
        /// Gets this variable's inlining cache.
        /// </summary>
        public VariableInliningCache InliningCache { get; private set; }

        public IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(new AnalyzedVariableGetBlock(this), Type);
        }

        public IStatement CreateReleaseStatement()
        {
            return new CodeBlockStatement(new AnalyzedVariableReleaseStatement(this));
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new CodeBlockStatement(new AnalyzedVariableSetBlock(this, (IAnalyzedExpression)Value.Emit(CodeGenerator)));
        }

        public IExpression CreateAddressOfExpression()
        {
            return new CodeBlockExpression(new AnalyzedVariableAddressOfBlock(this), Type.MakePointerType(PointerKind.ReferencePointer));
        }

        #region Variable Properties

        /// <summary>
        /// Gets the variable's properties.
        /// </summary>
        public virtual IVariableProperties Properties
        {
            get
            {
                return new VariableProperties(IsLocal);
            }
        }

        #endregion

        public abstract bool Equals(IAnalyzedVariable other);

        public override bool Equals(object obj)
        {
            if (obj is IAnalyzedVariable)
            {
                return this.Equals((IAnalyzedVariable)obj);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public abstract override int GetHashCode();
    }
}

using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public abstract class AnalysisVisitorBase : INodeVisitor, ILambdaVisitor
    {
        public SourceLocation CurrentLocation { get; protected set; }

        /// <summary>
        /// Analyzes the given statement. 
        /// A boolean value is returned that indicates if the
        /// analysis method has taken care of visiting its children,
        /// which means the visitor won't touch it anymore.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public abstract bool Analyze(IStatement Value);

        /// <summary>
        /// Analyzes the given statement. 
        /// A boolean value is returned that indicates if the
        /// analysis method has taken care of visiting its children,
        /// which means the visitor won't touch it anymore.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public abstract bool Analyze(IExpression Value);

        public IStatement Visit(IStatement Value)
        {
            if (Value == null)
            {
                return null;
            }

            var oldLoc = CurrentLocation;
            if (!Analyze(Value))
            {
                if (Value is SourceStatement)
                {
                    var newLoc = ((SourceStatement)Value).Location;
                    if (newLoc != null)
                    {
                        CurrentLocation = newLoc;
                    }
                }
                if (Value is IPredicateNode)
                {
                    ((IPredicateNode)Value).AcceptPredicate(this);
                }
                else
                {
                    Value.Accept(this);
                }
                CurrentLocation = oldLoc;
            }
            return Value;
        }

        public IExpression Visit(IExpression Value)
        {
            if (Value == null)
            {
                return null;
            }

            var oldLoc = CurrentLocation;
            if (!Analyze(Value))
            {
                if (Value is SourceExpression)
                {
                    var newLoc = ((SourceExpression)Value).Location;
                    if (newLoc != null)
                    {
                        CurrentLocation = newLoc;
                    }
                }
                if (Value is IPredicateNode)
                {
                    ((IPredicateNode)Value).AcceptPredicate(this);
                }
                else
                {
                    Value.Accept(this);
                }
            }
            CurrentLocation = oldLoc;
            return Value;
        }

        public IStatement VisitBody(IStatement Value, IMethod OwningMember)
        {
            return Visit(Value);
        }

        public IExpression VisitBody(IExpression Value, IMethod OwningMember)
        {
            return Visit(Value);
        }
    }
}

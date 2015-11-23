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
    public struct Bounds<T>
    {
        public Bounds(T Min, T Max)
        {
            this = default(Bounds<T>);
            this.Min = Min;
            this.Max = Max;
        }
        public Bounds(T Value)
            : this(Value, Value)
        { }

        public T Min { get; private set; }
        public T Max { get; private set; }
    }

    /// <summary>
    /// A node visitor that keeps track of the number of nodes that match a given pattern.
    /// </summary>
    public class NodeCountVisitor : IFlowVisitor<Bounds<InfiniteInt32>, Bounds<InfiniteInt32>>
    {
        public NodeCountVisitor(Func<INode, bool> Matches)
        {
            this.Matches = Matches;
            this.locs = new HashSet<SourceLocation>();
        }

        public Func<INode, bool> Matches { get; private set; }
        public Bounds<InfiniteInt32> CurrentFlow { get; set; }

        private SourceLocation currentLoc;
        private HashSet<SourceLocation> locs;
        public IEnumerable<SourceLocation> MatchLocations { get { return locs; } }

        public static Func<INode, bool> MatchCalls(Func<DissectedCall, bool> Matches)
        {
            return new Func<INode, bool>(node =>
            {
                var call = AnalysisHelpers.DissectCall(node as IExpression);
                return call != null && Matches(call);
            });
        }

        public Bounds<InfiniteInt32> CreateCollapsedFlow(Bounds<InfiniteInt32> First, Bounds<InfiniteInt32> Second)
        {
            return CreateSequenceFlow(First, Second);
        }

        public Bounds<InfiniteInt32> CreateDeltaFlow(Bounds<InfiniteInt32> First, Bounds<InfiniteInt32> Second)
        {
            return new Bounds<InfiniteInt32>(Second.Min - First.Min, Second.Max - First.Max);
        }

        public Bounds<InfiniteInt32> CreateLoopFlow(UniqueTag Tag, Bounds<InfiniteInt32> Body)
        {
            return new Bounds<InfiniteInt32>(0, Body.Max * InfiniteInt32.Infinity);
        }

        public Bounds<InfiniteInt32> CreateSelectFlow(Bounds<InfiniteInt32> First, Bounds<InfiniteInt32> Second)
        {
            return new Bounds<InfiniteInt32>(InfiniteInt32.Min(First.Min, Second.Min), InfiniteInt32.Max(First.Max, Second.Max));
        }

        public Bounds<InfiniteInt32> CreateSequenceFlow(Bounds<InfiniteInt32> First, Bounds<InfiniteInt32> Second)
        {
            return new Bounds<InfiniteInt32>(First.Min + Second.Min, First.Max + Second.Max);
        }

        public Bounds<InfiniteInt32> TerminatedFlow
        {
            get { return new Bounds<InfiniteInt32>(0); }
        }

        private void RegisterMatch()
        {
            CurrentFlow = new Bounds<InfiniteInt32>(CurrentFlow.Min + 1, CurrentFlow.Max + 1);
            if (currentLoc != null)
            {
                locs.Add(currentLoc);
            }
        }

        private void CheckMatch(INode Value)
        {
            if (Matches(Value))
            {
                RegisterMatch();
            }
        }

        private void UpdateLocation(SourceLocation Location)
        {
            if (Location != null)
            {
                currentLoc = Location;
            }
        }

        public IStatement Visit(IStatement Value)
        {
            var oldLoc = currentLoc;
            if (Value is SourceStatement)
            {
                UpdateLocation(((SourceStatement)Value).Location);
            }

            CheckMatch(Value);

            if (Value is IFlowStatement)
            {
                ((IFlowStatement)Value).AcceptFlow(this);
            }
            else if (Value is IPredicateNode)
            {
                ((IPredicateNode)Value).AcceptPredicate(this);
            }
            else
            {
                Value.Accept(this);
            }

            currentLoc = oldLoc;
            return Value;
        }

        public IExpression Visit(IExpression Value)
        {
            var oldLoc = currentLoc;
            if (Value is SourceExpression)
            {
                UpdateLocation(((SourceExpression)Value).Location);
            }

            CheckMatch(Value);

            if (Value is IFlowExpression)
            {
                ((IFlowExpression)Value).AcceptFlow(this);
            }
            else if (Value is IPredicateNode)
            {
                ((IPredicateNode)Value).AcceptPredicate(this);
            }
            else
            {
                Value.Accept(this);
            }

            currentLoc = oldLoc;
            return Value;
        }
    }
}

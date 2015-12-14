using Flame.Compiler;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode.Stack
{
    /// <summary>
    /// Describes a stack whose contents can be cached to variables to respect the order expressions and statements are executed in.
    /// </summary>
    public abstract class CachingExpressionStackBase
    {
        public CachingExpressionStackBase()
        {
            this.stack = new List<IExpression>();
            this.cachedCount = 0;
        }
        public CachingExpressionStackBase(IEnumerable<IExpression> Expressions)
        {
            this.stack = new List<IExpression>(Expressions);
            this.cachedCount = 0;
        }
        public CachingExpressionStackBase(CachingExpressionStackBase Other)
        {
            this.stack = new List<IExpression>(Other.stack);
            this.cachedCount = Other.cachedCount;
        }

        protected abstract CachedExpression CacheExpression(IExpression Expression, int Index);

        private List<IExpression> stack;
        private int cachedCount;

        public void Push(IExpression Value)
        {
            stack.Add(Value);
        }
        public void Push(CachingExpressionStackBase Stack)
        {
            foreach (var item in Stack.stack)
            {
                Push(item);
            }
        }
        public IExpression Peek()
        {
            return stack[stack.Count - 1];
        }
        public IExpression Pop()
        {
            var last = Peek();
            if (cachedCount == stack.Count)
            {
                cachedCount--;
            }
            stack.RemoveAt(stack.Count - 1);
            return last;
        }

        /// <summary>
        /// Orders the expression stack to cache all of its expressions, to preserve the order in which expression and statements are executed.
        /// </summary>
        public IStatement Cache()
        {
            var results = new List<IStatement>();
            for (int i = cachedCount; i < stack.Count; i++)
            {
                var cached = CacheExpression(stack[i], i);
                results.Add(cached.InitializedCacheStatement);
                stack[i] = cached.RetrieveExpression;
            }
            cachedCount = stack.Count;
            return new BlockStatement(results);
        }
    }
}

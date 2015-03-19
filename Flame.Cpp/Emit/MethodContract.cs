using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class MethodContract
    {
        public MethodContract(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.preconds = new List<PreconditionBlock>();
            this.postconds = new List<PostconditionBlock>();
        }

        private List<PreconditionBlock> preconds;
        private List<PostconditionBlock> postconds;
        private ICppBlock invariantCheck;

        public IType DeclaringType { get { return CodeGenerator.Method.DeclaringType; } }
        public IMethod Method { get { return CodeGenerator.Method; } }
        public ICodeGenerator CodeGenerator { get; private set; }

        private bool EmitInvariantPrecondition
        {
            get { return !Method.IsConstructor && Method.get_Access() != AccessModifier.Private; }
        }

        private bool EmitInvariantPostcondition
        {
            get { return Method.get_Access() != AccessModifier.Private; }
        }

        public void AddPrecondition(PreconditionBlock Block)
        {
            preconds.Add(Block);
        }

        public void AddPostcondition(PostconditionBlock Block)
        {
            postconds.Add(Block);
        }

        public IEnumerable<PreconditionBlock> Preconditions
        {
            get
            {
                return WithInvariantsAssertion(preconds, EmitInvariantPrecondition, block => new PreconditionBlock(block));
            }
        }
        public IEnumerable<PostconditionBlock> Postconditions
        {
            get
            {
                return WithInvariantsAssertion(postconds, EmitInvariantPostcondition, block => new PostconditionBlock(block));
            }
        }

        private ICppBlock GetInvariantsCheck()
        {
            if (invariantCheck == null)
            {
                var checkMethod = DeclaringType.GetInvariantsCheckMethod();
                if (checkMethod != null && !checkMethod.Equals(Method))
                {
                    invariantCheck = (ICppBlock)CodeGenerator.EmitInvocation(checkMethod, CodeGenerator.GetThis().CreateGetExpression().Emit(CodeGenerator), Enumerable.Empty<ICodeBlock>());
                }
            }
            return invariantCheck;
        }

        private IEnumerable<T> WithInvariantsAssertion<T>(IEnumerable<T> Assertions, bool EmitInvariant, Func<ICppBlock, T> AssertionBuilder)
        {
            if (!EmitInvariant)
            {
                return Assertions;
            }
            var check = GetInvariantsCheck();
            if (check == null)
            {
                return Assertions;
            }
            else
            {
                return Assertions.With(AssertionBuilder(check));
            }
        }

        /// <summary>
        /// Gets a boolean flag that indicates if this method contract has preconditions. 
        /// </summary>
        public bool HasPreconditions
        {
            get
            {
                return Preconditions.Any();
            }
        }

        /// <summary>
        /// Gets a boolean flag that indicates if this method contract has postconditions. 
        /// </summary>
        public bool HasPostconditions
        {
            get
            {
                return Postconditions.Any();
            }
        }

        /// <summary>
        /// Gets a sequence of precondition/postcondition description attributes.
        /// </summary>
        public IEnumerable<DescriptionAttribute> DescriptionAttributes
        {
            get
            {
                List<DescriptionAttribute> attrs = new List<DescriptionAttribute>();
                foreach (var item in Preconditions)
                {
                    attrs.Add(new DescriptionAttribute("pre", item.GetCode().ToString()));
                }
                foreach (var item in Postconditions)
                {
                    attrs.Add(new DescriptionAttribute("post", item.GetCode().ToString()));
                }
                return attrs;
            }
        }
    }
}

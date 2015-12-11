using Flame.Compiler;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// Defines a data structure that holds information
    /// pertaining to member signature passes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MemberSignaturePassArgument<T>
        where T : IMember
    {
        /// <summary>
        /// Creates a new member signature pass argument
        /// from the given member and pass environment.
        /// </summary>
        /// <param name="Member"></param>
        /// <param name="PassEnvironment"></param>
        public MemberSignaturePassArgument(T Member, IBodyPassEnvironment PassEnvironment)
        {
            this.Member = Member;
            this.PassEnvironment = PassEnvironment;
        }

        /// <summary>
        /// Gets the member signature this pass is
        /// being applied to.
        /// </summary>
        public T Member { get; private set; }

        /// <summary>
        /// Gets the pass environment for this pass.
        /// </summary>
        public IBodyPassEnvironment PassEnvironment { get; private set; }

        /// <summary>
        /// Gets the compiler log.
        /// </summary>
        public ICompilerLog Log { get { return PassEnvironment.Log; } }

        /// <summary>
        /// Gets the environment.
        /// </summary>
        public IEnvironment Environment { get { return PassEnvironment.Environment; } }
    }

    /// <summary>
    /// Gets the result of a member signature pass.
    /// </summary>
    public sealed class MemberSignaturePassResult
    {
        public MemberSignaturePassResult(string Name, IEnumerable<IAttribute> AdditionalAttributes)
        {
            this.Name = Name;
            this.AdditionalAttributes = AdditionalAttributes;
        }

        /// <summary>
        /// Gets the member's (new) name, if any.
        /// Otherwise, null.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a sequence of additional attributes that have
        /// been inferred.
        /// </summary>
        public IEnumerable<IAttribute> AdditionalAttributes { get; private set; }
    
        /// <summary>
        /// Combines the given member signature pass results.
        /// </summary>
        /// <param name="First"></param>
        /// <param name="Second"></param>
        /// <returns></returns>
        public static MemberSignaturePassResult Combine(MemberSignaturePassResult First, MemberSignaturePassResult Second)
        {
            return new MemberSignaturePassResult(Second.Name ?? First.Name, First.AdditionalAttributes.Union(Second.AdditionalAttributes));
        }

        public static readonly MemberSignaturePassResult Empty = new MemberSignaturePassResult(null, Enumerable.Empty<IAttribute>());
    }
}

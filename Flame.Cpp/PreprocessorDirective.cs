using Flame.Compiler;
using Flame.Cpp.Emit;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    /// <summary>
    /// A class that encapsulates a preprocessor directive.
    /// </summary>
    public class PreprocessorDirective : ISyntaxNode, IEquatable<PreprocessorDirective>
    {
        public PreprocessorDirective(string Name, params ISyntaxNode[] Arguments)
            : this(Name, (IEnumerable<ISyntaxNode>)Arguments)
        {

        }
        public PreprocessorDirective(string Name, IEnumerable<ISyntaxNode> Arguments)
        {
            this.Name = Name;
            this.Arguments = Arguments;
        }

        /// <summary>
        /// Gets the directive's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the arguments for this preprocessor directive.
        /// </summary>
        public IEnumerable<ISyntaxNode> Arguments { get; private set; }

        /// <summary>
        /// Gets the preprocessor directive's code.
        /// </summary>
        /// <returns></returns>
        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("#");
            cb.Append(Name);
            foreach (var item in Arguments)
            {
                cb.Append(" ");
                cb.Append(item.GetCode());
            }
            return cb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }

        public bool Equals(PreprocessorDirective other)
        {
            return this.ToString() == other.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is PreprocessorDirective)
            {
                return Equals((PreprocessorDirective)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        #region Static

        public static PreprocessorDirective PragmaOnce
        {
            get
            {
                return new PreprocessorDirective("pragma once");
            }
        }

        public static PreprocessorDirective CreateIncludeDirective(IHeaderDependency Dependency)
        {
            return new PreprocessorDirective("include", new IncludePath(Dependency));
        }

        #endregion
    }
}

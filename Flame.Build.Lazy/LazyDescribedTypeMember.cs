using System;
using Flame.Build;
using System.Collections.Generic;
using Flame.Compiler;
using System.Threading;

namespace Flame.Build.Lazy
{
    /// <summary>
    /// A base class for type members that are constructed lazily.
    /// </summary>
    public abstract class LazyDescribedTypeMember : LazyDescribedMember, ITypeMember
    {
        /// <summary>
        /// Initializes this lazily described member with the given name and
        /// declaring type.
        /// </summary>
        public LazyDescribedTypeMember(UnqualifiedName Name, IType DeclaringType)
            : base(Name)
        {
            this.DeclaringType = DeclaringType;
        }

        /// <summary>
        /// Gets the type that declared this member.
        /// </summary>
        /// <value>The type that declared this member.</value>
        public IType DeclaringType { get; private set; }

        /// <summary>
        /// Gets this member's qualified name.
        /// </summary>
        public sealed override QualifiedName FullName
        {
            get
            {
                if (DeclaringType == null)
                    return Name.Qualify();
                else
                    return Name.Qualify(DeclaringType.FullName);
            }
        }

        private bool isStaticVal;

        /// <summary>
        /// Gets or sets a boolean flag that indicates whether this type member
        /// is static or not.
        /// </summary>
        public bool IsStatic
        {
            get
            {
                CreateBody();
                return isStaticVal;
            }
            set
            {
                CreateBody();
                isStaticVal = value;
            }
        }
    }
}
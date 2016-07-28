using System;

namespace Flame.Build.Lazy
{
    /// <summary>
    /// An accessor implementation that constructs itself lazily.
    /// </summary>
    public class LazyDescribedAccessor : LazyDescribedMethod, IAccessor
    {
        /// <summary>
        /// Creates a new lazily described method from the given accessor type,
        /// the declaring property, and a deferred construction action.
        /// </summary>
        public LazyDescribedAccessor(
            AccessorType AccessorType, IProperty DeclaringProperty,
            Action<LazyDescribedAccessor> AnalyzeBody)
            : base(
                new SimpleName(
                    AccessorType.ToString().ToLower() + "_"
                    + DeclaringProperty.Name.ToString()),
                DeclaringProperty.DeclaringType,
                x => AnalyzeBody((LazyDescribedAccessor)x))
        {
            this.AccessorType = AccessorType;
            this.DeclaringProperty = DeclaringProperty;
        }

        /// <summary>
        /// Gets this accessor's type.
        /// </summary>
        public AccessorType AccessorType { get; private set; }

        /// <summary>
        /// Gets this accessor's declaring property.
        /// </summary>
        public IProperty DeclaringProperty { get; private set; }
    }
}

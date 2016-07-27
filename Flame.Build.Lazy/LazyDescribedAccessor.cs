using System;

namespace Flame.Build.Lazy
{
    public class LazyDescribedAccessor : LazyDescribedMethod, IAccessor
    {
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

        public AccessorType AccessorType { get; private set; }
        public IProperty DeclaringProperty { get; private set; }
    }
}


using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public abstract class StaticSystemTypeBase : IType
    {
        public INamespace DeclaringNamespace
        {
            get { return SystemNamespace.Instance; }
        }

        public abstract string Name { get; }
        public abstract IMethod[] GetMethods();

        public IEnumerable<IType> BaseTypes
        {
            get { return new IType[0]; }
        }

        public IBoundObject GetDefaultValue()
        {
            throw new InvalidOperationException();
        }

        public IEnumerable<IField> Fields
        {
            get { return new IField[0]; }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return new IProperty[0]; }
        }

        public IEnumerable<IMethod> Methods
        {
            get { return GetMethods(); }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[0]; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return new IGenericParameter[0]; }
        }

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }
    }
}
